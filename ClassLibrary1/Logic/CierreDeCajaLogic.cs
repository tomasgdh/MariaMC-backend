
using Data.Models;
using Entities.Items;
using Entities.RequestModels;
using Logic.ILogic;
using Microsoft.EntityFrameworkCore;
using Enumeradores;
using Entities.ResponseModels;
using System.Linq;
using System.Collections.Generic;
using Azure;

namespace Logic.Logic
{
    public class CierreDeCajaLogic : ICierreDeCajaLogic
    {
        private readonly Maria_MCContext _context;

        public CierreDeCajaLogic(Maria_MCContext context)
        {
            _context = context;
        }

        public async Task<CierreDeCajaResponse> CierreDeCaja(CierreDeCajaRequest cc) {

            using (var dbContextTransaction = await _context.Database.BeginTransactionAsync())
            {
                CierreDeCajaResponse cierre = new CierreDeCajaResponse();
                try
                {

                    // 1. Obtener la última fecha de cierre
                    var ultimoCierre = await _context.CierreDeCaja
                        .Where(c => c.IdSucursal == cc.idSucursal)
                        .OrderByDescending(c => c.Fechahasta)
                        .FirstOrDefaultAsync();

                    DateTime fechaDesde = ultimoCierre?.Fechahasta ?? new DateTime(2024, 1, 1);
                    DateTime fechaHasta = DateTime.Now;

                    // 2. Recuperar movimientos de cuenta corriente
                    var movimientos = await _context.CuentaCorrientes
                        .Where(m => m.IdSucursal == cc.idSucursal && m.Fecha >= fechaDesde && m.Fecha <= fechaHasta)
                        .ToListAsync();

                    if (movimientos != null && movimientos.Count == 0) {
                        cierre.IdCierre = -2;
                        return cierre;
                    }

                    var ventas = movimientos.Where(m => m.IdTipoDeMovimiento == "V").ToList();
                    var compras = movimientos.Where(m => m.IdTipoDeMovimiento == "C").ToList();
                    var gastos = movimientos.Where(m => m.IdTipoDeMovimiento == "G").ToList();
                    var ingresos = movimientos.Where(m => m.IdTipoDeMovimiento == "I").ToList();

                    // 3. Calcular totales
                    int cantidadDeVentas = ventas.Count;
                    int cantidadDePrendas = ventas.Sum(v =>
                    {
                        var detalleVenta = _context.VentaDetalles.Where(d => d.IdVenta == v.IdMovimiento).ToList();
                        return detalleVenta.Sum(d => d.Cantidad);
                    });

                     // 4. Calcular y guardar totales agrupados por tipo de forma de pago
                    var totalesFormasDePago = new Dictionary<int, decimal>();
                    decimal totalEfectivo = 0;
                    decimal totalMP = 0;
                    var tipoMP = await _context.TipoFormaDePago
                                            .Where(td => td.Descripcion.StartsWith("MP"))
                                            .Select(td => td.Id)
                                            .ToListAsync(); 
                    var tipoEfectivo = await _context.TipoFormaDePago
                                            .Where(td => td.Descripcion.StartsWith("EFECTIVO"))
                                            .Select(td => td.Id)
                                            .ToListAsync();
                                 
                    //TipoFormaPago[] tipoMP = new[] { TipoFormaPago.MPTransferencia, TipoFormaPago.MPQR, TipoFormaPago.MPDebito, TipoFormaPago.MPCredito };
                    //TipoFormaPago[] tipoEfectivo = new[] { TipoFormaPago.Efectivo, TipoFormaPago.Efectivo10Off };

                    var detalles = new List<DetalleTransaccion>();
                    // Procesar ventas
                    foreach (var venta in ventas)
                    {
                        var formasDePago = await _context.VentaFormasDePagos
                            .Include(vfp => vfp.IdTipoFormaPagoNavigation)
                            .Where(f => f.IdVenta == venta.IdMovimiento).ToListAsync();
                        foreach (var formaPago in formasDePago)
                        {
                            if (totalesFormasDePago.ContainsKey(formaPago.IdTipoFormaPago))
                            {
                                totalesFormasDePago[formaPago.IdTipoFormaPago] += formaPago.ValorParcial;
                            }
                            else
                            {
                                totalesFormasDePago[formaPago.IdTipoFormaPago] = formaPago.ValorParcial;
                            }
                            detalles.Add(new DetalleTransaccion
                            {
                                FechaHora = venta.Fecha.ToString("dd/MM/yyyy HH:mm:ss"),
                                IdMovimiento = venta.IdMovimiento,
                                TipoMovimiento = "Venta",
                                FormaDePago = formaPago.IdTipoFormaPagoNavigation.Descripcion,
                                Importe = formaPago.ValorParcial
                            });

                            if (tipoEfectivo.Contains(formaPago.IdTipoFormaPago))
                            {
                                totalEfectivo += formaPago.ValorParcial;
                            }
                            else if (tipoMP.Contains(formaPago.IdTipoFormaPago))
                            {
                                totalMP += formaPago.ValorParcial;
                            }
                        }


                    }

                    // Procesar compras
                    foreach (var compra in compras)
                    {
                        var formasDePago = await _context.CompraFormasDePago
                            .Include(c => c.IdTipoFormaPagoNavigation)
                            .Where(f => f.IdCompra == compra.IdMovimiento).ToListAsync();
                        foreach (var formaPago in formasDePago)
                        {
                            if (totalesFormasDePago.ContainsKey(formaPago.IdTipoFormaPago))
                            {
                                totalesFormasDePago[formaPago.IdTipoFormaPago] -= formaPago.Valor;
                            }
                            else
                            {
                                totalesFormasDePago[formaPago.IdTipoFormaPago] = -formaPago.Valor;
                            }
                            detalles.Add(new DetalleTransaccion
                            {
                                FechaHora = compra.Fecha.ToString("dd/MM/yyyy HH:mm:ss"),
                                IdMovimiento = compra.IdMovimiento,
                                TipoMovimiento = "Compra",
                                FormaDePago = formaPago.IdTipoFormaPagoNavigation.Descripcion,
                                Importe = -formaPago.Valor
                            });

                            if (tipoEfectivo.Contains(formaPago.IdTipoFormaPago))
                            {
                                totalEfectivo -= formaPago.Valor;
                            }
                            else if (tipoMP.Contains(formaPago.IdTipoFormaPago))
                            {
                                totalMP -= formaPago.Valor;
                            }
                        }
                    }
                    // Procesar Gastos
                    foreach (var gasto in gastos)
                    {
                        var formasDePago = await _context.Gasto
                            .Include(g => g.IdTipoFormaPagoNavigation)
                            .Where(f => f.Id == gasto.IdMovimiento).ToListAsync();
                        foreach (var formaPago in formasDePago)
                        {
                            if (totalesFormasDePago.ContainsKey(formaPago.IdTipoFormaPago))
                            {
                                totalesFormasDePago[formaPago.IdTipoFormaPago] -= formaPago.Importe;
                            }
                            else
                            {
                                totalesFormasDePago[formaPago.IdTipoFormaPago] = -formaPago.Importe;
                            }

                            detalles.Add(new DetalleTransaccion
                            {
                                FechaHora = gasto.Fecha.ToString("dd/MM/yyyy HH:mm:ss"),
                                IdMovimiento = gasto.IdMovimiento,
                                TipoMovimiento = "Gasto",
                                FormaDePago = formaPago.IdTipoFormaPagoNavigation.Descripcion,
                                Importe = -gasto.Importe
                            });
                            if (tipoEfectivo.Contains(formaPago.IdTipoFormaPago))
                            {
                                totalEfectivo -= gasto.Importe;
                            }
                            else if (tipoMP.Contains(formaPago.IdTipoFormaPago))
                            {
                                totalMP -= gasto.Importe;
                            }
                        }
                    }

                    // Procesar Ingresos
                    foreach (var ingreso in ingresos)
                    {
                        var formasDePago = await _context.Ingreso
                            .Include(i => i.IdTipoFormaPagoNavigation)
                            .Where(f => f.Id == ingreso.IdMovimiento).ToListAsync();
                        foreach (var formaPago in formasDePago)
                        {
                            if (totalesFormasDePago.ContainsKey(formaPago.IdTipoFormaPago))
                            {
                                totalesFormasDePago[formaPago.IdTipoFormaPago] += formaPago.Importe;
                            }
                            else
                            {
                                totalesFormasDePago[formaPago.IdTipoFormaPago] = formaPago.Importe;
                            }

                            detalles.Add(new DetalleTransaccion
                            {
                                FechaHora = ingreso.Fecha.ToString("dd/MM/yyyy HH:mm:ss"),
                                IdMovimiento = ingreso.IdMovimiento,
                                TipoMovimiento = "Ingreso",
                                FormaDePago = formaPago.IdTipoFormaPagoNavigation.Descripcion,
                                Importe = ingreso.Importe
                            });
                            if (tipoEfectivo.Contains(formaPago.IdTipoFormaPago))
                            {
                                totalEfectivo += ingreso.Importe;
                            }
                            else if (tipoMP.Contains(formaPago.IdTipoFormaPago))
                            {
                                totalMP += ingreso.Importe;
                            }
                        }
                    }

                    // 5. Insertar en CierreDecaja
                    decimal? enCaja = _context.TablaDeConfiguracion
                 .Where(tc => tc.Clave == "EnCaja" && tc.IdSucursal == cc.idSucursal)
                 .Select(tc => tc.ValorDecimal)
                 .FirstOrDefault();

                    decimal efEnCaja = enCaja.HasValue ? enCaja.Value : 0;
                    decimal ultimoCierreEfCaja = ultimoCierre?.CierreEfCaja ?? efEnCaja;

                    EfectivoEnCaja EfectivoCaja = calcularEfectivoCaja(efEnCaja, totalEfectivo, ultimoCierreEfCaja);

                    var cierreDecaja = new CierreDeCaja
                    {
                        IdSucursal = cc.idSucursal,
                        FechaDesde = fechaDesde,
                        Fechahasta = fechaHasta,
                        CantidadDeVentas = cantidadDeVentas,
                        CantidadDePrendas = cantidadDePrendas,
                        TotalEfectivo = totalEfectivo,
                        TotalMp = totalMP,
                        EnCaja = efEnCaja,
                        AperturaEfCaja = EfectivoCaja.CajaInicial,
                        CierreEfCaja = EfectivoCaja.CajaFinal,
                        EfectivoAGuardar = EfectivoCaja.AGuardar,
                        IdUsuario = cc.idUsuario
                    };
                    _context.CierreDeCaja.Add(cierreDecaja);
                    await _context.SaveChangesAsync();

                    // Guardar los detalles en CierreDeCajaDetalle
                    foreach (var total in totalesFormasDePago)
                    {
                        var detalle = new CierreDeCajaDetalle
                        {
                            IdCierre = cierreDecaja.IdCierre,
                            IdTipoFormaPago = total.Key,
                            Total = total.Value
                        };
                        await _context.CierreDeCajaDetalle.AddAsync(detalle);
                    }

                    // Actualizar IdCierre en CuentaCorriente
                    foreach (var movimiento in movimientos)
                    {
                        movimiento.IdCierre = cierreDecaja.IdCierre;
                    }

                    await _context.SaveChangesAsync();


                    var UsuarioCierre = await _context.Usuarios
                        .FirstOrDefaultAsync(c => c.IdUsuario == cc.idUsuario);

                    cierre = new CierreDeCajaResponse
                    {
                        IdCierre = cierreDecaja.IdCierre,
                        FechaDesde = fechaDesde.ToString("dd/MM/yyyy HH:mm:ss"),
                        FechaHasta = fechaHasta.ToString("dd/MM/yyyy HH:mm:ss"),
                        UsuarioCierre = UsuarioCierre?.NombreUsuario ?? "",
                        TotalEfectivo = totalEfectivo,
                        TotalMP = totalMP,
                        EnCaja = efEnCaja,
                        AperturaEfCaja = EfectivoCaja.CajaInicial,
                        CierreEfCaja = EfectivoCaja.CajaFinal,
                        EfectivoAGuardar = EfectivoCaja.AGuardar,
                        Detalle = detalles
                    };
                    // Commit de la transacción
                    await dbContextTransaction.CommitAsync();
                    return cierre;
                }
                catch (Exception e)
                {
                    // En caso de error, hacer rollback de la transacción
                    await dbContextTransaction.RollbackAsync();
                    // Manejar el error o lanzarlo nuevamente si es necesario
                    return cierre;
                }
            }


        }
        public async Task<CierreDeCajaResponse> CierreDeCajaX(CierreDeCajaRequest cc) {

            CierreDeCajaResponse cierre = new CierreDeCajaResponse();
            try
            {

                // 1. Obtener la última fecha de cierre
                var ultimoCierre = await _context.CierreDeCaja
                    .Where(c => c.IdSucursal == cc.idSucursal)
                    .OrderByDescending(c => c.Fechahasta)
                    .FirstOrDefaultAsync();

                DateTime fechaDesde = ultimoCierre?.Fechahasta ?? new DateTime(2024, 1, 1);
                DateTime fechaHasta = DateTime.Now;
                

                // 2. Recuperar movimientos de cuenta corriente
                var movimientos = await _context.CuentaCorrientes
                    .Where(m => m.IdSucursal == cc.idSucursal && m.Fecha >= fechaDesde && m.Fecha <= fechaHasta)
                    .ToListAsync();

                if (movimientos != null && movimientos.Count == 0)
                {
                    cierre.IdCierre = -2;
                    return cierre;
                }

                var ventas = movimientos.Where(m => m.IdTipoDeMovimiento == "V").ToList();
                var compras = movimientos.Where(m => m.IdTipoDeMovimiento == "C").ToList();
                var gastos = movimientos.Where(m => m.IdTipoDeMovimiento == "G").ToList();
                var ingresos = movimientos.Where(m => m.IdTipoDeMovimiento == "I").ToList();

                // 3. Calcular totales
                int cantidadDeVentas = ventas.Count;
                int cantidadDePrendas = ventas.Sum(v =>
                {
                    var detalleVenta = _context.VentaDetalles.Where(d => d.IdVenta == v.IdMovimiento).ToList();
                    return detalleVenta.Sum(d => d.Cantidad);
                });


                // 5. Calcular y guardar totales agrupados por tipo de forma de pago
                var totalesFormasDePago = new Dictionary<int, decimal>();
                decimal totalEfectivo = 0;
                decimal totalMP = 0;
                var tipoMP = await _context.TipoFormaDePago
                        .Where(td => td.Descripcion.StartsWith("MP"))
                        .Select(td => td.Id)
                        .ToListAsync();
                var tipoEfectivo = await _context.TipoFormaDePago
                                        .Where(td => td.Descripcion.StartsWith("EFECTIVO"))
                                        .Select(td => td.Id)
                                        .ToListAsync();

                //TipoFormaPago[] tipoMP = new[] { TipoFormaPago.MPTransferencia, TipoFormaPago.MPQR, TipoFormaPago.MPDebito, TipoFormaPago.MPCredito };
                //TipoFormaPago[] tipoEfectivo = new[] { TipoFormaPago.Efectivo, TipoFormaPago.Efectivo10Off };

                var detalles = new List<DetalleTransaccion>();

                // Procesar ventas
                foreach (var venta in ventas)
                {
                    var formasDePago = await _context.VentaFormasDePagos
                        .Include(vfp => vfp.IdTipoFormaPagoNavigation)
                        .Where(f => f.IdVenta == venta.IdMovimiento).ToListAsync();
                    foreach (var formaPago in formasDePago)
                    {
                        if (totalesFormasDePago.ContainsKey(formaPago.IdTipoFormaPago))
                        {
                            totalesFormasDePago[formaPago.IdTipoFormaPago] += formaPago.ValorParcial;
                        }
                        else
                        {
                            totalesFormasDePago[formaPago.IdTipoFormaPago] = formaPago.ValorParcial;
                        }
                        detalles.Add(new DetalleTransaccion
                        {
                            FechaHora = venta.Fecha.ToString("dd/MM/yyyy HH:mm:ss"),
                            IdMovimiento = venta.IdMovimiento,
                            TipoMovimiento = "Venta",
                            FormaDePago = formaPago.IdTipoFormaPagoNavigation.Descripcion,
                            Importe = formaPago.ValorParcial
                        });

                        if (tipoEfectivo.Contains(formaPago.IdTipoFormaPago))
                        {
                            totalEfectivo += formaPago.ValorParcial;
                        }
                        else if (tipoMP.Contains(formaPago.IdTipoFormaPago))
                        {
                            totalMP += formaPago.ValorParcial;
                        }
                    }


                }

                // Procesar compras
                foreach (var compra in compras)
                {
                    var formasDePago = await _context.CompraFormasDePago
                        .Include(c => c.IdTipoFormaPagoNavigation)
                        .Where(f => f.IdCompra == compra.IdMovimiento).ToListAsync();
                    foreach (var formaPago in formasDePago)
                    {
                        if (totalesFormasDePago.ContainsKey(formaPago.IdTipoFormaPago))
                        {
                            totalesFormasDePago[formaPago.IdTipoFormaPago] -= formaPago.Valor;
                        }
                        else
                        {
                            totalesFormasDePago[formaPago.IdTipoFormaPago] = -formaPago.Valor;
                        }
                        detalles.Add(new DetalleTransaccion
                        {
                            FechaHora = compra.Fecha.ToString("dd/MM/yyyy HH:mm:ss"),
                            IdMovimiento = compra.IdMovimiento,
                            TipoMovimiento = "Compra",
                            FormaDePago = formaPago.IdTipoFormaPagoNavigation.Descripcion,
                            Importe = -formaPago.Valor
                        });

                        if (tipoEfectivo.Contains(formaPago.IdTipoFormaPago))
                        {
                            totalEfectivo -= formaPago.Valor;
                        }
                        else if (tipoMP.Contains(formaPago.IdTipoFormaPago))
                        {
                            totalMP -= formaPago.Valor;
                        }
                    }
                }
                // Procesar Gastos
                foreach (var gasto in gastos)
                {
                    var formasDePago = await _context.Gasto
                        .Include(g => g.IdTipoFormaPagoNavigation)
                        .Where(f => f.Id == gasto.IdMovimiento).ToListAsync();
                    foreach (var formaPago in formasDePago)
                    {
                        if (totalesFormasDePago.ContainsKey(formaPago.IdTipoFormaPago))
                        {
                            totalesFormasDePago[formaPago.IdTipoFormaPago] -= formaPago.Importe;
                        }
                        else
                        {
                            totalesFormasDePago[formaPago.IdTipoFormaPago] = -formaPago.Importe;
                        }

                        detalles.Add(new DetalleTransaccion
                        {
                            FechaHora = gasto.Fecha.ToString("dd/MM/yyyy HH:mm:ss"),
                            IdMovimiento = gasto.IdMovimiento,
                            TipoMovimiento = "Gasto",
                            FormaDePago = formaPago.IdTipoFormaPagoNavigation.Descripcion,
                            Importe = -gasto.Importe
                        });
                        if (tipoEfectivo.Contains(formaPago.IdTipoFormaPago))
                        {
                            totalEfectivo -= gasto.Importe;
                        }
                        else if (tipoMP.Contains(formaPago.IdTipoFormaPago))
                        {
                            totalMP -= gasto.Importe;
                        }
                    }
                }

                // Procesar Ingresos
                foreach (var ingreso in ingresos)
                {
                    var formasDePago = await _context.Ingreso
                        .Include(i =>i.IdTipoFormaPagoNavigation)
                        .Where(f => f.Id == ingreso.IdMovimiento).ToListAsync();
                    foreach (var formaPago in formasDePago)
                    {
                        if (totalesFormasDePago.ContainsKey(formaPago.IdTipoFormaPago))
                        {
                            totalesFormasDePago[formaPago.IdTipoFormaPago] += formaPago.Importe;
                        }
                        else
                        {
                            totalesFormasDePago[formaPago.IdTipoFormaPago] = formaPago.Importe;
                        }

                        detalles.Add(new DetalleTransaccion
                        {
                            FechaHora = ingreso.Fecha.ToString("dd/MM/yyyy HH:mm:ss"),
                            IdMovimiento = ingreso.IdMovimiento,
                            TipoMovimiento = "Ingreso",
                            FormaDePago = formaPago.IdTipoFormaPagoNavigation.Descripcion,
                            Importe = ingreso.Importe
                        });
                        if (tipoEfectivo.Contains(formaPago.IdTipoFormaPago))
                        {
                            totalEfectivo += ingreso.Importe;
                        }
                        else if (tipoMP.Contains(formaPago.IdTipoFormaPago))
                        {
                            totalMP += ingreso.Importe;
                        }
                    }
                }

                decimal? enCaja = _context.TablaDeConfiguracion
                                 .Where(tc => tc.Clave == "EnCaja" && tc.IdSucursal == cc.idSucursal)
                                 .Select(tc => tc.ValorDecimal)
                                 .FirstOrDefault();

                decimal efEnCaja = enCaja.HasValue ? enCaja.Value : 0;
                decimal ultimoCierreEfCaja = ultimoCierre?.CierreEfCaja ?? efEnCaja;

                EfectivoEnCaja EfectivoCaja = calcularEfectivoCaja(efEnCaja, totalEfectivo, ultimoCierreEfCaja);                

                cierre = new CierreDeCajaResponse
                {
                    IdCierre = 0,
                    FechaDesde = fechaDesde.ToString("dd/MM/yyyy HH:mm:ss"),
                    FechaHasta = fechaHasta.ToString("dd/MM/yyyy HH:mm:ss"),
                    UsuarioCierre = "",
                    TotalEfectivo = totalEfectivo,
                    TotalMP = totalMP,
                    EnCaja = efEnCaja,
                    AperturaEfCaja = EfectivoCaja.CajaInicial,
                    CierreEfCaja = EfectivoCaja.CajaFinal,
                    EfectivoAGuardar = EfectivoCaja.AGuardar,
                    Detalle = detalles
                };

                return cierre;
            }
            catch (Exception e)
            {
                return cierre;
            }
        


        }
        private EfectivoEnCaja calcularEfectivoCaja(decimal enCaja, decimal TotalVentasEf, decimal CierreDeCajaAnterior) {

            EfectivoEnCaja efectivo = new EfectivoEnCaja();
            // calculo de fectivo.CajaInicial 
            efectivo.CajaInicial = CierreDeCajaAnterior;

            if (TotalVentasEf > 0)
            {
                //Calculo de Caja Final
                if (efectivo.CajaInicial == enCaja)
                {
                    efectivo.CajaFinal = efectivo.CajaInicial;
                    efectivo.AGuardar = efectivo.CajaInicial + TotalVentasEf - enCaja;
                }
                else if (efectivo.CajaInicial < enCaja)
                {
                    if (efectivo.CajaInicial + TotalVentasEf < enCaja)
                    {
                         efectivo.CajaFinal = efectivo.CajaInicial + TotalVentasEf;
                         efectivo.AGuardar = 0;
                    }
                    else
                    {
                        efectivo.CajaFinal = enCaja;
                        efectivo.AGuardar = efectivo.CajaInicial + TotalVentasEf - enCaja;
                    }
                }
            }
            else //TotalVentasEf <= 0
            {
                //si es negativo a guardar es 0
                efectivo.AGuardar = 0;
                //Calculo de Caja Final
                efectivo.CajaFinal = efectivo.CajaInicial + TotalVentasEf;
            }

            return efectivo;

        }

        public async Task<CierreDeCajaResponse> CalcularDetallesCierreDeCaja(long idCierre, DateTime fechaDesde, DateTime fechaHasta, string usuarioCierre)
        {
        decimal totalEfectivo = 0;
        decimal totalMP = 0;

            var tipoMP = await _context.TipoFormaDePago
                    .Where(td => td.Descripcion.StartsWith("MP"))
                    .Select(td => td.Id)
                    .ToListAsync();
            var tipoEfectivo = await _context.TipoFormaDePago
                                    .Where(td => td.Descripcion.StartsWith("EFECTIVO"))
                                    .Select(td => td.Id)
                                    .ToListAsync();

            //TipoFormaPago[] tipoMP = new[] { TipoFormaPago.MPTransferencia, TipoFormaPago.MPQR, TipoFormaPago.MPDebito, TipoFormaPago.MPCredito };
            //TipoFormaPago[] tipoEfectivo = new[] { TipoFormaPago.Efectivo, TipoFormaPago.Efectivo10Off };

            var movimientos = await _context.CuentaCorrientes
                .Where(m => m.IdCierre == idCierre)
                .ToListAsync();

            var detalles = new List<DetalleTransaccion>();

            foreach (var cc in movimientos)
            {
                if (cc.IdTipoDeMovimiento == "V")
                {
                    var venta = await _context.Venta
                        .Include(v => v.VentaFormasDePagos)
                        .ThenInclude(vfp => vfp.IdTipoFormaPagoNavigation)
                        .FirstOrDefaultAsync(v => v.IdVenta == cc.IdMovimiento);

                    if (venta != null)
                    {
                        foreach (var formaDePago in venta.VentaFormasDePagos)
                        {
                            detalles.Add(new DetalleTransaccion
                            {
                                FechaHora = cc.Fecha.ToString("dd/MM/yyyy HH:mm:ss"),
                                IdMovimiento = cc.IdMovimiento,
                                TipoMovimiento = "Venta",
                                FormaDePago = formaDePago.IdTipoFormaPagoNavigation.Descripcion,
                                Importe = formaDePago.ValorParcial
                            });

                            if (tipoEfectivo.Contains(formaDePago.IdTipoFormaPago))
                            {
                                totalEfectivo += formaDePago.ValorParcial;
                            }
                            else if (tipoMP.Contains(formaDePago.IdTipoFormaPago))
                            {
                                totalMP += formaDePago.ValorParcial;
                            }
                        }
                    }
                }
                else if (cc.IdTipoDeMovimiento == "C")
                {
                    var compra = await _context.Compras
                        .Include(c => c.CompraFormasDePago)
                        .ThenInclude(c => c.IdTipoFormaPagoNavigation)
                        .FirstOrDefaultAsync(c => c.IdCompra == cc.IdMovimiento);

                    if (compra != null)
                    {
                        foreach (var formaDePago in compra.CompraFormasDePago)
                        {
                            detalles.Add(new DetalleTransaccion
                            {
                                FechaHora = cc.Fecha.ToString("dd/MM/yyyy HH:mm:ss"),
                                IdMovimiento = cc.IdMovimiento,
                                TipoMovimiento = "Compra",
                                FormaDePago = formaDePago.IdTipoFormaPagoNavigation.Descripcion,
                                Importe = -formaDePago.Valor
                            });

                            if (tipoEfectivo.Contains(formaDePago.IdTipoFormaPago))
                            {
                                totalEfectivo -= formaDePago.Valor;
                            }
                            else if (tipoMP.Contains(formaDePago.IdTipoFormaPago))
                            {
                                totalMP -= formaDePago.Valor;
                            }
                        }
                    }
                }
                else if (cc.IdTipoDeMovimiento == "G")
                {
                    var gasto = await _context.Gasto
                        .FirstOrDefaultAsync(g => g.Id == cc.IdMovimiento);

                    if (gasto != null)
                    {
                        detalles.Add(new DetalleTransaccion
                        {
                            FechaHora = cc.Fecha.ToString("dd/MM/yyyy HH:mm:ss"),
                            IdMovimiento = cc.IdMovimiento,
                            TipoMovimiento = "Gasto",
                            FormaDePago = gasto.IdTipoFormaPagoNavigation.Descripcion,
                            Importe = -gasto.Importe
                        });
                        if (tipoEfectivo.Contains(gasto.IdTipoFormaPago))
                        {
                            totalEfectivo -= gasto.Importe;
                        }
                        else if (tipoMP.Contains(gasto.IdTipoFormaPago))
                        {
                            totalMP -= gasto.Importe;
                        }
                    }
                }
                else if (cc.IdTipoDeMovimiento == "I")
                {
                    var ingreso = await _context.Ingreso
                        .FirstOrDefaultAsync(g => g.Id == cc.IdMovimiento);

                    if (ingreso != null)
                    {
                        detalles.Add(new DetalleTransaccion
                        {
                            FechaHora = cc.Fecha.ToString("dd/MM/yyyy HH:mm:ss"),
                            IdMovimiento = cc.IdMovimiento,
                            TipoMovimiento = "Ingreso",
                            FormaDePago = ingreso.IdTipoFormaPagoNavigation.Descripcion,
                            Importe = ingreso.Importe
                        });
                        if (tipoEfectivo.Contains(ingreso.IdTipoFormaPago))
                        {
                            totalEfectivo += ingreso.Importe;
                        }
                        else if (tipoMP.Contains(ingreso.IdTipoFormaPago))
                        {
                            totalMP += ingreso.Importe;
                        }
                    }
                }
            }

            var CierreDeCaja = new CierreDeCajaResponse
            {
                IdCierre = idCierre,
                FechaDesde = fechaDesde.ToString("dd/MM/yyyy HH:mm:ss"),
                FechaHasta = fechaHasta.ToString("dd/MM/yyyy HH:mm:ss"),
                UsuarioCierre = usuarioCierre,
                TotalEfectivo = totalEfectivo,
                TotalMP = totalMP,
                Detalle = detalles
            };

            return CierreDeCaja;
        }


        public async Task<CierreDeCajaResponse> GetCierreDeCaja(long idCierre)
        {
            try {

                var CierreCaja = await _context.CierreDeCaja
                 .Include(c => c.IdUsuarioNavigation)
                 .FirstOrDefaultAsync(c => c.IdCierre == idCierre);

                decimal totalEfectivo = 0;
                decimal totalMP = 0;

                var tipoMP = await _context.TipoFormaDePago
                        .Where(td => td.Descripcion.StartsWith("MP"))
                        .Select(td => td.Id)
                        .ToListAsync();
                var tipoEfectivo = await _context.TipoFormaDePago
                                        .Where(td => td.Descripcion.StartsWith("EFECTIVO"))
                                        .Select(td => td.Id)
                                        .ToListAsync();

                //TipoFormaPago[] tipoMP = new[] { TipoFormaPago.MPTransferencia, TipoFormaPago.MPQR, TipoFormaPago.MPDebito, TipoFormaPago.MPCredito };
                //TipoFormaPago[] tipoEfectivo = new[] { TipoFormaPago.Efectivo, TipoFormaPago.Efectivo10Off };

                var movimientos = await _context.CuentaCorrientes
                        .Where(m => m.IdCierre == idCierre)
                        .ToListAsync();

                var detalles = new List<DetalleTransaccion>();

                foreach (var cc in movimientos)
                {
                    if (cc.IdTipoDeMovimiento == "V")
                    {
                        var venta = await _context.Venta
                            .Include(v => v.VentaFormasDePagos)
                            .ThenInclude(vfp => vfp.IdTipoFormaPagoNavigation)
                            .FirstOrDefaultAsync(v => v.IdVenta == cc.IdMovimiento);

                        if (venta != null)
                        {
                            foreach (var formaDePago in venta.VentaFormasDePagos)
                            {
                                detalles.Add(new DetalleTransaccion
                                {
                                    FechaHora = cc.Fecha.ToString("dd/MM/yyyy HH:mm:ss"),
                                    IdMovimiento = cc.IdMovimiento,
                                    TipoMovimiento = "Venta",
                                    FormaDePago = formaDePago.IdTipoFormaPagoNavigation.Descripcion,
                                    Importe = formaDePago.ValorParcial
                                });

                                if (tipoEfectivo.Contains(formaDePago.IdTipoFormaPago))
                                {
                                    totalEfectivo += formaDePago.ValorParcial;
                                }
                                else if (tipoMP.Contains(formaDePago.IdTipoFormaPago))
                                {
                                    totalMP += formaDePago.ValorParcial;
                                }
                            }
                        }
                    }
                    else if (cc.IdTipoDeMovimiento == "C")
                    {
                        var compra = await _context.Compras
                            .Include(c => c.CompraFormasDePago)
                            .ThenInclude(c => c.IdTipoFormaPagoNavigation)
                            .FirstOrDefaultAsync(c => c.IdCompra == cc.IdMovimiento);

                        if (compra != null)
                        {
                            foreach (var formaDePago in compra.CompraFormasDePago)
                            {
                                detalles.Add(new DetalleTransaccion
                                {
                                    FechaHora = cc.Fecha.ToString("dd/MM/yyyy HH:mm:ss"),
                                    IdMovimiento = cc.IdMovimiento,
                                    TipoMovimiento = "Compra",
                                    FormaDePago = formaDePago.IdTipoFormaPagoNavigation.Descripcion,
                                    Importe = -formaDePago.Valor
                                });

                                if (tipoEfectivo.Contains(formaDePago.IdTipoFormaPago))
                                {
                                    totalEfectivo -= formaDePago.Valor;
                                }
                                else if (tipoMP.Contains(formaDePago.IdTipoFormaPago))
                                {
                                    totalMP -= formaDePago.Valor;
                                }
                            }
                        }
                    }
                    else if (cc.IdTipoDeMovimiento == "G")
                    {
                        var gasto = await _context.Gasto
                            .FirstOrDefaultAsync(g => g.Id == cc.IdMovimiento);

                        if (gasto != null)
                        {
                            detalles.Add(new DetalleTransaccion
                            {
                                FechaHora = cc.Fecha.ToString("dd/MM/yyyy HH:mm:ss"),
                                IdMovimiento = cc.IdMovimiento,
                                TipoMovimiento = "Gasto",
                                FormaDePago = gasto.IdTipoFormaPagoNavigation.Descripcion,
                                Importe = -gasto.Importe
                            });
                            if (tipoEfectivo.Contains(gasto.IdTipoFormaPago))
                            {
                                totalEfectivo -= gasto.Importe;
                            }
                            else if (tipoMP.Contains(gasto.IdTipoFormaPago))
                            {
                                totalMP -= gasto.Importe;
                            }
                        }
                    }
                    else if (cc.IdTipoDeMovimiento == "I")
                    {
                        var ingreso = await _context.Ingreso
                            .FirstOrDefaultAsync(g => g.Id == cc.IdMovimiento);

                        if (ingreso != null)
                        {
                            detalles.Add(new DetalleTransaccion
                            {
                                FechaHora = cc.Fecha.ToString("dd/MM/yyyy HH:mm:ss"),
                                IdMovimiento = cc.IdMovimiento,
                                TipoMovimiento = "Ingreso",
                                FormaDePago = ingreso.IdTipoFormaPagoNavigation.Descripcion,
                                Importe = ingreso.Importe
                            });
                            if (tipoEfectivo.Contains(ingreso.IdTipoFormaPago))
                            {
                                totalEfectivo += ingreso.Importe;
                            }
                            else if (tipoMP.Contains(ingreso.IdTipoFormaPago))
                            {
                                totalMP += ingreso.Importe;
                            }
                        }
                    }
                }
                if (CierreCaja == null)
                {
                    throw new Exception("El Cierre De Caja No Existe");
                }

                var CierreDeCaja = new CierreDeCajaResponse
                {
                    IdCierre = idCierre,
                    FechaDesde = CierreCaja.FechaDesde.ToString("dd/MM/yyyy HH:mm:ss"),
                    FechaHasta = CierreCaja.Fechahasta.ToString("dd/MM/yyyy HH:mm:ss"),
                    UsuarioCierre = CierreCaja.IdUsuarioNavigation.NombreUsuario,
                    TotalEfectivo = totalEfectivo,
                    TotalMP = totalMP,
                    EnCaja = CierreCaja.EnCaja,
                    AperturaEfCaja = CierreCaja.AperturaEfCaja,
                    CierreEfCaja = CierreCaja.CierreEfCaja,
                    EfectivoAGuardar = CierreCaja.EfectivoAGuardar,
                    Detalle = detalles
                };

                return CierreDeCaja;
            }
            catch {
                return new CierreDeCajaResponse();
            }
        }

        public async Task<CierreDeCajaResponse> GetUltimoCierreDeCaja() {

            try
            {

                var CierreCaja = await _context.CierreDeCaja
                    .OrderBy(c => c.IdCierre)
                    .LastOrDefaultAsync();

                if (CierreCaja == null)
                {
                    throw new Exception("El Cierre De Caja No Existe");
                }

                return await GetCierreDeCaja(CierreCaja.IdCierre);
            }
            catch
            {
                return new CierreDeCajaResponse();
            }

        }

        public async Task<List<CCResponseDetalle>> GetAllCierreDeCaja(int idSucursal)
        {
            try
            {

                var cierres = await _context.CierreDeCaja
                    .Include(c => c.IdUsuarioNavigation)
                    .Where(c => c.IdSucursal == idSucursal)
                    .OrderBy(c => c.IdCierre)
                    .ToListAsync();

                if (cierres.Count  == 0)
                {
                    throw new Exception("El Cierre De Caja No Existe");
                }
                List<CCResponseDetalle> listaDeCierres = new List<CCResponseDetalle>();
                foreach (var cierre in cierres) {
                    CCResponseDetalle cc = new CCResponseDetalle
                    {
                        IdCierre = cierre.IdCierre,
                        FechaDesde = cierre.FechaDesde.ToString("dd/MM/yyyy HH:mm:ss"),
                        FechaHasta = cierre.Fechahasta.ToString("dd/MM/yyyy HH:mm:ss"),
                        Usuario = cierre.IdUsuarioNavigation.NombreUsuario,
                        CantidadDePrendas = cierre.CantidadDePrendas,
                        CantidadDeVentas = cierre.CantidadDeVentas,
                        TotalEfectivo = cierre.TotalEfectivo,
                        TotalMP = cierre.TotalMp
                    };
                    listaDeCierres.Add(cc);
                }


                return listaDeCierres;
            }
            catch
            {
                return new List <CCResponseDetalle>();
            }

        }
        public async Task<CCResponse> GetAllCierreDeCajaPaginado(int idSucursal, int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var totalRecords = await _context.CierreDeCaja.Where(c => c.IdSucursal == idSucursal).CountAsync();

                var listaDeCierres = await _context.CierreDeCaja
                        .Include(c => c.IdUsuarioNavigation)
                        .Where(c => c.IdSucursal == idSucursal)
                        .OrderByDescending(c => c.IdCierre)
                        .Skip((pageNumber - 1) * pageSize)
                        .Take(pageSize)
                         .Select(cierre =>
                                    new CCResponseDetalle
                                    {
                                        IdCierre = cierre.IdCierre,
                                        FechaDesde = cierre.FechaDesde.ToString("dd/MM/yyyy HH:mm:ss"),
                                        FechaHasta = cierre.Fechahasta.ToString("dd/MM/yyyy HH:mm:ss"),
                                        Usuario = cierre.IdUsuarioNavigation.NombreUsuario,
                                        CantidadDePrendas = cierre.CantidadDePrendas,
                                        CantidadDeVentas = cierre.CantidadDeVentas,
                                        TotalEfectivo = cierre.TotalEfectivo,
                                        TotalMP = cierre.TotalMp
                                    })
                        .ToListAsync();

                if (listaDeCierres.Count == 0)
                {
                    throw new Exception("El Cierre De Caja No Existe");
                }

                CCResponse response = new CCResponse
                {
                    totalDeRegistros = totalRecords,
                    Cierres = listaDeCierres
                };

                return response;
            }
            catch
            {
                CCResponse response = new CCResponse
                {
                    totalDeRegistros = 0,
                    Cierres = new List<CCResponseDetalle>()
                };

                return response;
            }

        }       
    }

    internal class EfectivoEnCaja
    {
        public decimal CajaInicial { get; set; }
        public decimal CajaFinal { get; set; }
        public decimal AGuardar { get; set; }
    }
}
