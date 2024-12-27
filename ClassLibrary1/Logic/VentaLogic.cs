
using Data.Models;
using Entities.Items;
using Entities.RequestModels;
using Logic.ILogic;
using Microsoft.EntityFrameworkCore;
using Enumeradores;
using Entities.ResponseModels;
using MercadoPago.Resource.User;
using Microsoft.EntityFrameworkCore.Internal;

namespace Logic.Logic
{
    public class VentaLogic : IVentaLogic
    {
        private readonly Maria_MCContext _context;

        public VentaLogic(Maria_MCContext context)
        {
            _context = context;
        }

        public async Task<long> RealizarVenta(VentaRequest venta) {

            using (var dbContextTransaction = _context.Database.BeginTransaction())
            {
                try
                {
                    // 1. Agregar la venta en la tabla Venta
                    Venta nuevaVenta = new Venta
                    {
                        IdSucursal = venta.idSucursal,
                        FechaVenta = DateTime.Now,
                        IdCliente = venta.idCliente,
                        TotalDeVenta = venta.total,
                        //IdComprobante = idComprobante, // Asegúrate de tener el idComprobante correcto
                        CreatedDate = DateTime.Now,
                        ModifiedDate = DateTime.Now,
                        IdUsuario = venta.idUsuario
                    };

                    _context.Venta.Add(nuevaVenta);
                    await _context.SaveChangesAsync();

                    // 2. Agregar los productos vendidos en la tabla VentaDetalle
                    foreach (var producto in venta.listaDeProductos)
                    {
                        VentaDetalle ventaDetalle = new VentaDetalle
                        {
                            IdVenta = nuevaVenta.IdVenta,
                            IdProducto = producto.id,
                            Cantidad = 1, // Asumiendo que se vende 1 unidad de cada producto
                            Valor = producto.price
                        };

                        _context.VentaDetalles.Add(ventaDetalle);

                        // 5. Actualizar el stock y el estado del producto en la tabla Productos
                        var productoDB = await _context.Productos.FindAsync(producto.id);
                        if (productoDB != null)
                        {
                            productoDB.Stock--;
                            productoDB.IdEstado = (int)TipoEstadoEnum.Vendido;
                            _context.Entry(productoDB).State = EntityState.Modified;
                            productoDB.ModifiedDate = DateTime.Now;
                            productoDB.IdUsuario = venta.idUsuario;
                        }                        

                        // 6. Editar el estado del producto en la tabla ProductoEstado
                        ProductoEstado? estadoAnterior = await _context.ProductoEstado
                            .Where(pe => pe.IdProducto == producto.id && pe.FechaFin == null)
                            .FirstOrDefaultAsync();

                        if (estadoAnterior != null)
                        {
                            estadoAnterior.FechaFin = DateTime.Now;
                            _context.Entry(estadoAnterior).State = EntityState.Modified;
                        }

                        ProductoEstado nuevoEstado = new ProductoEstado
                        {
                            IdProducto = producto.id,
                            IdEstado = (int)TipoEstadoEnum.Vendido,
                            FechaInicio = DateTime.Now,
                            IdUsuario = venta.idUsuario
                        };

                        _context.ProductoEstado.Add(nuevoEstado);
                    }

                    await _context.SaveChangesAsync();
                    decimal pagoCreditoEnTienda = 0;
                    // 3. Agregar los medios de pago en la tabla VentaFormasDePago
                    foreach (var formaPago in venta.listaMediosDePago)
                    {
                        VentaFormasDePago formaPagoVenta = new VentaFormasDePago
                        {
                            IdVenta = nuevaVenta.IdVenta,
                            IdTipoFormaPago = formaPago.id,
                            ValorParcial = formaPago.total
                        };

                        if (formaPago.id == (int)TipoFormaPago.CREDITO_EN_TIENDA)
                            pagoCreditoEnTienda = formaPago.total;

                        _context.VentaFormasDePagos.Add(formaPagoVenta);
                    }

                    await _context.SaveChangesAsync();


                    // Antes de realizar cualquier operación, obtenemos y bloqueamos el último registro de CuentaCorriente
                    var cuentaCorrienteUltimoMov = await _context.CuentaCorrientes
                        .FromSqlRaw("SELECT TOP 1 * FROM CuentaCorriente WITH (UPDLOCK) WHERE IdSucursal = {0} ORDER BY Fecha DESC", venta.idSucursal)
                        .FirstOrDefaultAsync();

                    if (cuentaCorrienteUltimoMov == null)
                    {
                        cuentaCorrienteUltimoMov = new CuentaCorriente
                        {
                            SaldoActual = 0 // Si no existen registros previos, se asume un saldo inicial de 0
                        };
                    }
                    decimal saldoAnterior = cuentaCorrienteUltimoMov.SaldoActual;
                    decimal nuevoSaldo = saldoAnterior + venta.total - pagoCreditoEnTienda;

                    // 4. Agregar el movimiento de la venta en la tabla CuentaCorriente
                    CuentaCorriente NuevaCuentaCorriente = new CuentaCorriente
                    {
                        IdSucursal = venta.idSucursal,
                        Fecha = DateTime.Now,
                        IdMovimiento = nuevaVenta.IdVenta,
                        Importe = venta.total - pagoCreditoEnTienda,
                        SaldoActual = nuevoSaldo, // Asegúrate de tener el saldo actual correcto
                        IdTipoDeMovimiento = "V",
                        CreatedDate = DateTime.Now,
                        ModifiedDate = DateTime.Now
                    };

                    _context.CuentaCorrientes.Add(NuevaCuentaCorriente);

                    await _context.SaveChangesAsync();

                    // 8. Actualizar el saldo del cliente si corresponde
                    var formaPagoCreditoEnTienda = venta.listaMediosDePago.FirstOrDefault(p => p.id == ((int)TipoFormaPago.CREDITO_EN_TIENDA));
                    if (formaPagoCreditoEnTienda != null)
                    {
                        var cliente = await _context.Clientes.FindAsync(venta.idCliente);
                        if (cliente != null)
                        {
                            cliente.SaldoEnCuenta -= formaPagoCreditoEnTienda.subTotal;

                            _context.Entry(cliente).State = EntityState.Modified;
                            await _context.SaveChangesAsync();
                        }

                    }
                    // Commit de la transacción
                    dbContextTransaction.Commit();

                    return nuevaVenta.IdVenta;
                }
                catch (Exception)
                {
                    // En caso de error, hacer rollback de la transacción
                    dbContextTransaction.Rollback();
                    // Manejar el error o lanzarlo nuevamente si es necesario
                    return -1;
                }
            }


        }

        public async Task<ListadoVentasResponse> GetAllVentasPaginado(int idSucursal, DateTime fechaDesde, DateTime fechaHasta, int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var totalRecords = await _context.Venta
                    .Where(c => c.IdSucursal == idSucursal
                                && (c.FechaVenta >= fechaDesde)
                                && (c.FechaVenta <= fechaHasta))
                    .CountAsync();

                var query = from v in _context.Venta
                            join c in _context.Comprobante on v.IdComprobante equals c.IdComprobante into gjComprobantes
                            from comprobante in gjComprobantes.DefaultIfEmpty()  // Simula el LEFT JOIN con Comprobante
                            join cli in _context.Clientes on v.IdCliente equals cli.IdCliente into gjClientes
                            from cliente in gjClientes.DefaultIfEmpty()  // Simula el LEFT JOIN con Cliente
                            where (comprobante == null || comprobante.IdSucursal == idSucursal)
                                  && (v.FechaVenta >= fechaDesde)
                                  && (v.FechaVenta <= fechaHasta)
                            select new VentaResultado
                            {
                                IdVenta = v.IdVenta,
                                IdSucursal = v.IdSucursal,
                                FechaVenta = v.FechaVenta.ToString("dd/MM/yyyy HH:mm:ss"),
                                TotalDeVenta = v.TotalDeVenta,
                                Comprobante = comprobante != null
                                    ? string.Format("{0:D4}-{1:D8}", comprobante.NroPuntoVenta, comprobante.NroComprobante)
                                    : string.Empty,
                                FacturaHtml = comprobante != null ? comprobante.FacturaHtml : string.Empty,
                                Cliente = cliente != null
                                    ? cliente.Apellido + ", " + cliente.Nombre
                                    : string.Empty,
                                NroDocumento = cliente != null ? cliente.NroDocumento : string.Empty
                            };

                var resultado = await query
                    .OrderByDescending(v => v.IdVenta)
                    .Skip((pageNumber - 1) * pageSize) // Paginación
                    .Take(pageSize) // Tamaño de página
                    .ToListAsync();

                if (resultado.Count == 0)
                {
                    throw new Exception("No hay ventas para mostrar");
                }

                ListadoVentasResponse response = new ListadoVentasResponse
                {
                    totalDeRegistros = totalRecords,
                    ventas = resultado
                };

                return response;
            }
            catch
            {
                ListadoVentasResponse response = new ListadoVentasResponse
                {
                    totalDeRegistros = 0,
                    ventas = new List<VentaResultado>()
                };

                return response;
            }
        }

        //public string GenerarFactura(string tablaProductos)
        //{
        //    // Ruta del archivo HTML
        //    string rutaPlantilla = "/PlantillaFactura/PlantillaFactura.html";

        //    // Cargar el contenido del archivo HTML
        //    string htmlFactura = File.ReadAllText(rutaPlantilla);

        //    // Reemplazar los placeholders con valores dinámicos
        //    htmlFactura = htmlFactura.Replace("{TipoFactura}", "A")
        //                             .Replace("{NombreFantasia}", "Empresa Fantástica")
        //                             .Replace("{RazonSocial}", "Razón Social S.A.")
        //                             .Replace("{Domicilio}", "Calle Falsa 123")
        //                             .Replace("{CondicionFrenteAlIva}", "Responsable Inscripto")
        //                             .Replace("{NroPuntoDeVenta}", "0001")
        //                             .Replace("{NroComprobante}", "12345678")
        //                             .Replace("{FechaEmision}", DateTime.Now.ToString("dd/MM/yyyy"))
        //                             .Replace("{CUIT}", "30-12345678-9")
        //                             .Replace("{IIBB}", "123456789")
        //                             .Replace("{FechaInicioActividades}", "01/01/2000")
        //                             .Replace("{CuitCliente}", "20-87654321-0")
        //                             .Replace("{NomYApeCliente}", "Juan Pérez")
        //                             .Replace("{CondicionFrenteAlIvaCliente}", "Consumidor Final")
        //                             .Replace("{DomicilioCliente}", "Av. Siempre Viva 456")
        //                             .Replace("{tablaProductos}", tablaProductos)
        //                             .Replace("{SubTotal}", "1000,00")
        //                             .Replace("{Total}", "1210,00")
        //                             .Replace("{CodigoQr}", "/ruta/a/imagen/qr.png")
        //                             .Replace("{CAE}", "12345678901234")
        //                             .Replace("{FechaVtoCAE}", DateTime.Now.AddDays(30).ToString("dd/MM/yyyy"));

        //    return htmlFactura;
        //}

        //public async Task<object> GenerarComprobanteYEnviarAlCliente(Int64 idVenta) 
        //{
        //    try
        //    {
        //        // 1. Obtener la información de la venta
        //        var venta = await _context.Venta.FirstAsync(v => v.IdVenta == idVenta);
        //        string contenidoHtml = "";
        //        if (venta != null)
        //        {
        //            var detalles = _context.VentaDetalles
        //                .Include(p => p.IdProductoNavigation)
        //                .Include(p => p.IdProductoNavigation.IdTipoProductoNavigation)
        //                .Include(p => p.IdProductoNavigation.IdTipoMarcaNavigation)
        //                .Include(p => p.IdProductoNavigation.IdTipoProductoNavigation)
        //                .Where(d => d.IdVenta == idVenta).ToList();

        //            var formasDePago = _context.VentaFormasDePagos
        //                .Include(vfp => vfp.IdTipoFormaPagoNavigation).Where(f => f.IdVenta == idVenta).ToList();

        //            // 2. Crear el contenido del PDF usando la plantilla
        //            var cliente = _context.Clientes.Find(venta.IdCliente);
        //            var usuario = _context.Usuarios.Find(venta.IdUsuario);
        //            string detalleProductos = string.Join("<br>", detalles.Select(d =>
        //            $"# {d.IdProducto} - {d.IdProductoNavigation.IdTipoMarcaNavigation.Descripcion} - {d.IdProductoNavigation.IdTipoProductoNavigation.Descripcion}  - Cantidad: {d.Cantidad} - Valor: {d.Valor:C2}"));

        //            string detalleMediosDePago = string.Join("<br>", formasDePago.Select(f =>
        //                $"{f.IdTipoFormaPagoNavigation.Descripcion} - {f.ValorParcial:C2}"));

        //            // 2. Calcular el total de productos y formas de pago
        //            var totalProductos = detalles.Sum(d => d.Cantidad * d.Valor);
        //            var totalFormasDePago = formasDePago.Sum(f => f.ValorParcial);

        //            // Calcular la diferencia como descuento
        //            var descuento = totalProductos - totalFormasDePago;

        //            //contenidoHtml = $@"
        //            //    <br>
        //            //    Maria Moda Circular<br>
        //            //    --------------------------------<br>
        //            //    Cliente: {cliente.Apellido}, {cliente.Nombre}<br>
        //            //    Mail: {cliente.Mail}<br>
        //            //    --------------------------------<br>
        //            //    Fecha: {venta.FechaVenta}<br>
        //            //    Operación: {venta.IdVenta}<br>
        //            //    Atendido por: {usuario.NombreUsuario}<br>
        //            //    --------------------------------<br>
        //            //    Producto:<br>
        //            //    {detalleProductos}<br>
        //            //    --------------------------------<br>
        //            //    Forma de Pago:<br>
        //            //    {detalleMediosDePago}<br>
        //            //    --------------------------------<br>
        //            //    SubTotal: {totalProductos:C2}<br>
        //            //    --------------------------------<br>
        //            //    Descuento: {descuento:C2}<br>
        //            //    --------------------------------<br>
        //            //    Total: {venta.TotalDeVenta:C2}<br>
        //            //    --------------------------------<br>
        //            //    Gracias por su compra.<br>
        //            //    <br>
        //            //    <br>
        //            //    <br>
        //            //    <br>
        //            //    <br>
        //            //    <br>";

        //            contenidoHtml = $@"
        //            <!DOCTYPE html>
        //            <html lang=""es"">
        //            <head>
        //                <meta charset=""UTF-8"">
        //                <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
        //                <title>Factura</title>
        //            </head>
        //            <body style=""font-size: 0.7rem;"">
        //                <header>
        //                    <!-- Aquí puedes colocar contenido adicional para el encabezado si es necesario -->
        //                </header>
        //                <main>
        //                    <h3 style=""font-size: 1rem;"">Maria Moda Circular</h3>
        //                    <hr>
        //                    <p style=""font-size: 0.7rem;""><b>Cliente:</b> {cliente.Apellido}, {cliente.Nombre}</p>
        //                    <p style=""font-size: 0.7rem;""><b>Mail:</b>  {cliente.Mail}</p>           
        //                    <hr>
        //                    <p style=""font-size: 0.7rem;""><b>Fecha:</b> {venta.FechaVenta}</p>
        //                    <p style=""font-size: 0.7rem;""><b>Operación:</b> {venta.IdVenta}</p>
        //                    <p style=""font-size: 0.7rem;""><b>Atendido por:</b> {usuario.NombreUsuario}</p>
        //                    <hr>
        //                    <div style=""font-size: 0.7rem;"">{generarTablaProductosDetalle(detalles)}</div>
        //                    <hr>
        //                    <div style=""font-size: 0.7rem;"">{generarTablaFormasDePago(formasDePago)}</div>
        //                    <hr>                            
        //                    <div style=""font-size: 0.7rem;"">{generarTablatotales(totalProductos, descuento, venta.TotalDeVenta)}</div>
        //                    <hr>
        //                    <p style=""font-size: 0.7rem;"">Gracias por su compra.</p>
        //                </main>
        //                <footer>
        //                    <!-- Aquí puedes colocar contenido adicional para el pie de página si es necesario -->
        //                </footer>
        //            </body>
        //            </html>";


        //            SendMailLogic sendMailLogic = new SendMailLogic();
        //            await sendMailLogic.SendEmailAsync(cliente.Mail,"Maria Moda Circular - Compra N° "+ venta.IdVenta.ToString() , contenidoHtml, "");

        //            // 3. Generar el PDF
        //            //using (var ms = new MemoryStream())
        //            //{
        //            //    Document doc = new Document();
        //            //    PdfWriter writer = PdfWriter.GetInstance(doc, ms);
        //            //    doc.Open();

        //            //    using (var htmlWorker = new iTextSharp.text.html.simpleparser.HTMLWorker(doc))
        //            //    {
        //            //        using (var sr = new StringReader(contenidoHtml))
        //            //        {
        //            //            htmlWorker.Parse(sr);
        //            //        }
        //            //    }

        //            //    doc.Close();

        //            //    // 4. Enviar el correo con el PDF adjunto
        //            //    using (var smtpClient = new SmtpClient("smtp.gmail.com", 587))
        //            //    {
        //            //        smtpClient.Credentials = new System.Net.NetworkCredential("tuemail@gmail.com", "tucontraseña");
        //            //        smtpClient.EnableSsl = true;

        //            //        MailMessage mail = new MailMessage();
        //            //        mail.From = new MailAddress("tuemail@gmail.com");
        //            //        mail.To.Add(emailCliente);
        //            //        mail.Subject = "Comprobante de Venta";
        //            //        mail.Body = "Adjunto encontrarás tu comprobante de venta.";

        //            //        mail.Attachments.Add(new Attachment(new MemoryStream(ms.ToArray()), "ComprobanteVenta.pdf"));

        //            //        smtpClient.Send(mail);
        //            //    }
        //            //}
        //        }
        //        else {
        //            return new { result = "error", message = "Ocurrio un error al procesar el envio del comprobante. Venta no encontrada ("+ idVenta.ToString()+ ")" };
        //        }
        //        return new { result = "ok", message = "El archivo se proceso correctamente", comprobante = contenidoHtml };

        //    }
        //    catch (Exception)
        //    {
        //        return new { result = "error", message = "Ocurrio un error al procesar el envio del comprobante" };
        //    }            

        //}
        //public string generarTablaProductosDetalle(List<VentaDetalle> detalle) {


        //    string detalleProductos = string.Join("", detalle.Select(d =>
        //            $@"
        //                    <tr>
        //                        <td>{d.IdProducto}</td>
        //                        <td>{d.IdProductoNavigation.IdTipoProductoNavigation.Descripcion} - {d.IdProductoNavigation.IdTipoMarcaNavigation.Descripcion}</td>
        //                        <td>{d.Cantidad:F2}</td>
        //                        <td>Unidad</td>
        //                        <td>{d.Valor:C2}</td>
        //                        <td>{0:P2}</td>
        //                        <td>{0:C2}</td>
        //                        <td>{(d.Cantidad * d.Valor):C2}</td>
        //                    </tr>
        //                    "));

        //    string tablaHTML = $@"
        //                <table>
        //                    <tr>
        //                        <td>Código</td>
        //                        <td>Producto</td>
        //                        <td>Cantidad</td>
        //                        <td>U. Medida</td>
        //                        <td>Precio Unit.</td>
        //                        <td>% Bonif.</td>
        //                        <td>Imp. Bonif.</td>
        //                        <td>Subtotal</td>
        //                    </tr>
        //                    {detalleProductos}
        //                </table>";



        //    //string tablaHTML = "<table style='border-collapse: collapse; font-size: 0.7rem;'>";
        //    //tablaHTML += "<thead><tr>";

        //    //// Encabezados de la tabla con fondo gris claro
        //    //tablaHTML += "<th style='padding: 5px; border: 1px solid black; background-color: #f2f2f2; '>Código</th>";
        //    //tablaHTML += "<th style='padding: 5px; border: 1px solid black; background-color: #f2f2f2; '>Producto</th>";
        //    //tablaHTML += "<th style='padding: 5px; border: 1px solid black; background-color: #f2f2f2;'>Precio</th>";
        //    //tablaHTML += "</tr></thead><tbody>";
        //    //decimal total = 0;
        //    //// Filas de la tabla
        //    //foreach (var item in detalle)
        //    //{
        //    //    tablaHTML += "<tr>";
        //    //    tablaHTML += $@"<td style = 'padding: 5px; border: 1px solid black;' >#{item.IdProducto}</td>";
        //    //    tablaHTML += $@"<td style = 'padding: 5px; border: 1px solid black;' >{item.IdProductoNavigation.IdTipoMarcaNavigation.Descripcion} - {item.IdProductoNavigation.IdTipoProductoNavigation.Descripcion} </td>";
        //    //    tablaHTML += $@"<td style = 'padding: 5px; border: 1px solid black; text-align: right;' >{item.Valor:C2}</td>";
        //    //    tablaHTML += "</tr>";
        //    //    total += item.Valor; 
        //    //}

        //    //tablaHTML += "<tr>";
        //    //tablaHTML += "<td style = 'padding: 5px; border: 1px solid black;font-weight: bold;' colspan= 2 > TOTAL </td>";
        //    //tablaHTML += $@"<td style = 'padding: 5px; border: 1px solid black; text-align: right;font-weight: bold;' >{ total:C2}</td>";
        //    //tablaHTML += "</tr>";

        //    //tablaHTML += "</tbody></table>";

        //    return tablaHTML;
        //}

        //public string generarTablaFormasDePago(List<VentaFormasDePago> detalle)
        //{
        //    string tablaHTML = "<table style='border-collapse: collapse; font-size: 0.7rem;'>";
        //    tablaHTML += "<thead><tr>";

        //    // Encabezados de la tabla con fondo gris claro
        //    tablaHTML += "<th style='padding: 5px; border: 1px solid black; background-color: #f2f2f2; '>Medios de pago:</th>";
        //    tablaHTML += "<th style='padding: 5px; border: 1px solid black; background-color: #f2f2f2; '>Valor</th>";
        //    tablaHTML += "</tr></thead><tbody>";
        //    decimal total = 0;
        //    // Filas de la tabla
        //    foreach (var item in detalle)
        //    {
        //        tablaHTML += "<tr>";
        //        tablaHTML += $@"<td style = 'padding: 5px; border: 1px solid black;' >{item.IdTipoFormaPagoNavigation.Descripcion}</td>";
        //        tablaHTML += $@"<td style = 'padding: 5px; border: 1px solid black; text-align: right;' >{item.ValorParcial:C2}</td>";
        //        tablaHTML += "</tr>";
        //        total += item.ValorParcial;
        //    }

        //    tablaHTML += "<tr>";
        //    tablaHTML += "<td style = 'padding: 5px; border: 1px solid black;font-weight: bold;' > TOTAL </td>";
        //    tablaHTML += $@"<td style = 'padding: 5px; border: 1px solid black; text-align: right;font-weight: bold;' >{total:C2}</td>";
        //    tablaHTML += "</tr>";

        //    tablaHTML += "</tbody></table>";

        //    return tablaHTML;
        //}
        //public string generarTablatotales(decimal totalProductos,decimal descuento,decimal TotalDeVenta)
        //{
        //    string tablaHTML = "<table style='border-collapse: collapse; font-size: 0.7rem;'>";
        //    tablaHTML += "<tbody>";
        //    tablaHTML += "<tr>";
        //    tablaHTML += "<td style = 'padding: 5px; border: 1px solid black;' > SUBTOTAL </td>";
        //    tablaHTML += $@"<td style = 'padding: 5px; border: 1px solid black; text-align: right;' >{totalProductos:C2}</td>";
        //    tablaHTML += "</tr>";
        //    tablaHTML += "<tr>";
        //    tablaHTML += "<td style = 'padding: 5px; border: 1px solid black;' > DESCUENTO </td>";
        //    tablaHTML += $@"<td style = 'padding: 5px; border: 1px solid black; text-align: right;' >{descuento:C2}</td>";
        //    tablaHTML += "</tr>";
        //    tablaHTML += "<tr>";
        //    tablaHTML += "<td style = 'padding: 5px; border: 1px solid black;background-color: #f2f2f2;font-weight: bold;' > TOTAL </td>";
        //    tablaHTML += $@"<td style = 'padding: 5px; border: 1px solid black; text-align: right;background-color: #f2f2f2;font-weight: bold;' >{TotalDeVenta:C2}</td>";
        //    tablaHTML += "</tr>";

        //    tablaHTML += "</tbody></table>";

        //    return tablaHTML;
        //}
    }
}
