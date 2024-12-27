
using Data.Models;
using Entities.Items;
using Entities.RequestModels;
using Logic.ILogic;
using Microsoft.EntityFrameworkCore;
using Enumeradores;

namespace Logic.Logic
{
    public class CompraLogic : ICompraLogic
    {
        private readonly Maria_MCContext _context;

        public CompraLogic(Maria_MCContext context)
        {
            _context = context;
        }

        public async Task<long> RealizarCompra(CompraRequest compra) {

            using (var dbContextTransaction = _context.Database.BeginTransaction())
            {
                try
                {
                    // 1. Agregar la venta en la tabla Venta
                    Compra nuevaCompra = new Compra
                    {
                        IdSucursal = compra.idSucursal,
                        FechaCompra = DateTime.Now,
                        IdCliente = compra.idCliente,
                        TotalDeCompra = compra.totalEfectivo,
                        TotalCreditoEnTienda = compra.totalCredito,
                        //IdComprobante = idComprobante, // Asegúrate de tener el idComprobante correcto
                        CreatedDate = DateTime.Now,
                        ModifiedDate = DateTime.Now,
                        IdUsuario = compra.idUsuario
                    };

                    _context.Compras.Add(nuevaCompra);
                    await _context.SaveChangesAsync();

                    // 2. Agregar los productos vendidos en la tabla VentaDetalle
                    foreach (var prod in compra.listaDeProductos)
                    {
                        Producto producto = new Producto
                        {
                            IdEstado = (int)TipoEstadoEnum.Ingresado,
                            //Descripcion = prod.descripcion,
                            Stock = 1,
                            CreatedDate = DateTime.Now,
                            ModifiedDate = DateTime.Now,
                            IdUsuario = compra.idUsuario,
                            IdTipoProducto = prod.idCategoria,
                            IdTipoTalle = prod.idTalle,
                            IdTipoMarca = prod.idMarca,
                            PrecioDeCompra = prod.ValorCompra,
                            PrecioDeVenta = Math.Round(prod.ValorVentaSugerido * 1.2m,2)
                        };

                        _context.Productos.Add(producto);
                        await _context.SaveChangesAsync();

                        CompraDetalle compraDetalle = new CompraDetalle
                        {
                            IdCompra = nuevaCompra.IdCompra,
                            IdProducto = producto.IdProducto,
                            Cantidad = 1, // Asumiendo que se vende 1 unidad de cada producto
                            
                        };

                        _context.CompraDetalles.Add(compraDetalle);
                                         

                        // 6. Editar el estado del producto en la tabla ProductoEstado
                        ProductoEstado? estadoAnterior = await _context.ProductoEstado
                            .Where(pe => pe.IdProducto == producto.IdProducto && pe.FechaFin == null)
                            .FirstOrDefaultAsync();

                        if (estadoAnterior != null)
                        {
                            estadoAnterior.FechaFin = DateTime.Now;
                            _context.Entry(estadoAnterior).State = EntityState.Modified;
                        }

                        ProductoEstado nuevoEstado = new ProductoEstado
                        {
                            IdProducto = producto.IdProducto,
                            IdEstado = (int)TipoEstadoEnum.Ingresado,
                            FechaInicio = DateTime.Now,
                            IdUsuario = compra.idUsuario
                        };

                        _context.ProductoEstado.Add(nuevoEstado);
                    }

                    await _context.SaveChangesAsync();

                    // 3. Agregar los medios de pago en la tabla VentaFormasDePago
                    foreach (var formaPago in compra.listaMediosDePago)
                    {
                        CompraFormasDePago formaDePago = new CompraFormasDePago
                        {
                            IdCompra = nuevaCompra.IdCompra,
                            IdTipoFormaPago = formaPago.id,
                            Valor = formaPago.total
                        };

                        _context.CompraFormasDePago.Add(formaDePago);
                    }

                    await _context.SaveChangesAsync();


                    // Antes de realizar cualquier operación, obtenemos y bloqueamos el último registro de CuentaCorriente
                    var cuentaCorrienteUltimoMov = await _context.CuentaCorrientes
                        .FromSqlRaw("SELECT TOP 1 * FROM CuentaCorriente WITH (UPDLOCK) WHERE IdSucursal = {0} ORDER BY Fecha DESC", compra.idSucursal)
                        .FirstOrDefaultAsync();

                    if (cuentaCorrienteUltimoMov == null)
                    {
                        cuentaCorrienteUltimoMov = new CuentaCorriente
                        {
                            SaldoActual = 0 // Si no existen registros previos, se asume un saldo inicial de 0
                        };
                    }
                    decimal saldoAnterior = cuentaCorrienteUltimoMov.SaldoActual;
                    decimal nuevoSaldo = saldoAnterior - compra.totalEfectivo;

                    // 4. Agregar el movimiento de la venta en la tabla CuentaCorriente
                    CuentaCorriente NuevaCuentaCorriente = new CuentaCorriente
                    {
                        IdSucursal = compra.idSucursal,
                        Fecha = DateTime.Now,
                        IdMovimiento = nuevaCompra.IdCompra,
                        Importe = compra.totalEfectivo,
                        SaldoActual = nuevoSaldo, // Asegúrate de tener el saldo actual correcto
                        IdTipoDeMovimiento = "C",
                        CreatedDate = DateTime.Now,
                        ModifiedDate = DateTime.Now
                    };

                    _context.CuentaCorrientes.Add(NuevaCuentaCorriente);

                    await _context.SaveChangesAsync();

                    // 8. Actualizar el saldo del cliente si corresponde
                    var formaPagoCreditoEnTienda = compra.listaMediosDePago.FirstOrDefault(p => p.id == ((int)TipoFormaPago.CREDITO_EN_TIENDA));
                    if (formaPagoCreditoEnTienda != null)
                    {
                        var cliente = await _context.Clientes.FindAsync(compra.idCliente);
                        if (cliente != null)
                        {
                            cliente.SaldoEnCuenta += formaPagoCreditoEnTienda.total;

                            _context.Entry(cliente).State = EntityState.Modified;
                            await _context.SaveChangesAsync();
                        }

                    }

                    // Commit de la transacción
                    dbContextTransaction.Commit();
                    return nuevaCompra.IdCompra;
                }
                catch (Exception e)
                {
                    // En caso de error, hacer rollback de la transacción
                    dbContextTransaction.Rollback();
                    // Manejar el error o lanzarlo nuevamente si es necesario
                    return -1;
                }
            }


        }
    }
}
