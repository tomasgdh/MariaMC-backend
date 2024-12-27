using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Data.Models;
using Entities.Items;
using Entities.RequestModels;
using Enumeradores;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace WebApiMariaMC.Controllers
{

    [Route("api/[controller]/[action]")]
    [ApiController]
    public class GastoController : ControllerBase
    {
        private readonly Maria_MCContext _context;  // Ajusta esto con el nombre correcto de tu DbContext

        public GastoController(Maria_MCContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<object>> GetAll(int idSucursal)
        {
            try
            {
                var lista = await _context.Gasto
                    .Include(vfp => vfp.IdTipoDeGastoNavigation)
                    .Include(vfp => vfp.IdTipoFormaPagoNavigation)
                    .Where(c => c.IdSucursal == idSucursal)
                                   .Select(td => 
                                    new { id = td.Id,
                                        //idSucursal = td.IdSucursal,
                                        fecha = td.Fecha.ToString("dd/MM/yyyy"),
                                        descripcion = td.Descripcion,
                                        idTipoMovimiento = td.IdTipoDeGasto,
                                        tipoMovimientoDesc = td.IdTipoDeGastoNavigation.Descripcion,
                                        idTipoFormaDePago = td.IdTipoFormaPago,
                                        descTipoFormaDePago = td.IdTipoFormaPagoNavigation.Descripcion,
                                        importe = td.Importe
                                   }).OrderBy(m => m.id)
                                   .ToListAsync();
                if (lista.Count == 0)
                {
                    return new { result = "error", message = "No hay datos para mostrar" };
                }

                return new { result = "ok", lista };
            }
            catch (Exception ex)
            {

                return new { result = "error", message = "Ocurrio un error. Exception: " + ex.Message };

            }
        }
        [HttpGet]
        public async Task<ActionResult<object>> GetAllPaginado(int idSucursal, int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var totalRecords = await _context.Gasto.Where(c => c.IdSucursal == idSucursal).CountAsync();
                var lista = await _context.Gasto
                    .Include(vfp => vfp.IdTipoDeGastoNavigation)
                    .Include(vfp => vfp.IdTipoFormaPagoNavigation)
                    .Where(c => c.IdSucursal == idSucursal)
                    .Select(td =>
                    new {
                        id = td.Id,
                        //idSucursal = td.IdSucursal,
                        fecha = td.Fecha.ToString("dd/MM/yyyy"),
                        descripcion = td.Descripcion,
                        idTipoMovimiento = td.IdTipoDeGasto,
                        tipoMovimientoDesc = td.IdTipoDeGastoNavigation.Descripcion,
                        idTipoFormaDePago = td.IdTipoFormaPago,
                        descTipoFormaDePago = td.IdTipoFormaPagoNavigation.Descripcion,
                        importe = td.Importe
                    }).OrderByDescending(c => c.id)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
                if (lista.Count == 0)
                {
                    return new { result = "error", message = "No hay datos para mostrar" };
                }

                return new { result = "ok", lista, totalRecords };
            }
            catch (Exception ex)
            {

                return new { result = "error", message = "Ocurrio un error. Exception: " + ex.Message };

            }
        }

        [NonAction]
        [HttpGet(Name = "GetGasto")]
        public async Task<ActionResult<object>> Get(int id)
        {
            var tipo = await _context.Gasto.FindAsync(id);

            if (tipo == null)
            {
                return new { result = "error", message = "Item inexistente Id: " + id.ToString() };
            }

            return new { result = "ok", item = tipo };
        }

        [HttpPost]
        public async Task<ActionResult<object>> Add(ItemMovimientoDeCajaRequest itemDTO)
        {
            using (var dbContextTransaction = _context.Database.BeginTransaction())
            {
                try
                {
                    Gasto item = new Gasto
                    {
                        IdSucursal = itemDTO.idSucursal,
                        Fecha = DateTime.Now,
                        Descripcion = itemDTO?.descripcion,
                        IdTipoDeGasto = itemDTO.idTipoMovimiento,
                        IdTipoFormaPago = itemDTO.idTipoFormaDePago,
                        Importe = itemDTO.importe,
                        CreatedDate = DateTime.Now,
                        IdUsuario = itemDTO.idUsuario,
                    };
                    _context.Gasto.Add(item);
                    await _context.SaveChangesAsync();

                    // Recargar el item con las propiedades de navegación
                    item = await _context.Gasto
                        .Include(g => g.IdTipoDeGastoNavigation)
                        .Include(g => g.IdTipoFormaPagoNavigation)
                        .FirstOrDefaultAsync(g => g.Id == item.Id);

                    var nuevoItem = new
                    {
                        item.Id,
                        Fecha = item.Fecha.ToString("dd/MM/yyyy"),
                        item.Descripcion,
                        item.IdTipoDeGasto,
                        tipoMovimientoDesc = item.IdTipoDeGastoNavigation.Descripcion,
                        item.IdTipoFormaPago,
                        descTipoFormaDePago = item.IdTipoFormaPagoNavigation.Descripcion,
                        item.Importe
                    };

                    // Antes de realizar cualquier operación, obtenemos y bloqueamos el último registro de CuentaCorriente
                    var cuentaCorrienteUltimoMov = await _context.CuentaCorrientes
                        .FromSqlRaw("SELECT TOP 1 * FROM CuentaCorriente WITH (UPDLOCK) WHERE IdSucursal = {0} ORDER BY Fecha DESC", itemDTO.idSucursal)
                        .FirstOrDefaultAsync();

                    if (cuentaCorrienteUltimoMov == null)
                    {
                        cuentaCorrienteUltimoMov = new CuentaCorriente
                        {
                            SaldoActual = 0 // Si no existen registros previos, se asume un saldo inicial de 0
                        };
                    }
                    decimal saldoAnterior = cuentaCorrienteUltimoMov.SaldoActual;
                    decimal nuevoSaldo = saldoAnterior - item.Importe;

                    // 4. Agregar el movimiento de la venta en la tabla CuentaCorriente
                    CuentaCorriente NuevaCuentaCorriente = new CuentaCorriente
                    {
                        IdSucursal = itemDTO.idSucursal,
                        Fecha = item.Fecha,
                        IdMovimiento = item.Id,
                        Importe = item.Importe,
                        SaldoActual = nuevoSaldo, // Asegúrate de tener el saldo actual correcto
                        IdTipoDeMovimiento = "G",
                        CreatedDate = DateTime.Now,
                        ModifiedDate = DateTime.Now,
                    };

                    _context.CuentaCorrientes.Add(NuevaCuentaCorriente);

                    await _context.SaveChangesAsync();




                    // Commit de la transacción
                    dbContextTransaction.Commit();
                    return new { result = "ok", item = nuevoItem };
                }
                catch (Exception ex)
                {
                    dbContextTransaction.Rollback();
                    return new { result = "error", message = "Ocurrio un error. Exception: " + ex.Message };
                }
            }
              
        }

        [HttpPost]
        public async Task<ActionResult<object>> Update(ItemMovimientoDeCajaRequest itemDTO)
        {
            using (var dbContextTransaction = _context.Database.BeginTransaction())
            {
                try
                {
                    Gasto? tdAModificar = await _context.Gasto.FindAsync(itemDTO.id);
                    if (tdAModificar == null)
                    {
                        return new { result = "error", message = "Item inexistente Id: " + itemDTO.id.ToString() };
                    }

                    CuentaCorriente? cc = await _context.CuentaCorrientes
                        .Where(i => i.IdMovimiento == itemDTO.id
                        && i.IdTipoDeMovimiento == "G" /*Gasto*/
                        && i.IdCierre == null).FirstOrDefaultAsync();

                    if (cc != null)
                    {
                        return new { result = "error", message = "No se puede modificar el Item porque ya pertenece a un cierre" };
                    }

                    //tdAModificar.IdSucursal = itemDTO.idSucursal;
                    tdAModificar.Descripcion = itemDTO.descripcion;
                    tdAModificar.IdTipoDeGasto = itemDTO.idTipoMovimiento;
                    tdAModificar.IdTipoFormaPago = itemDTO.idTipoFormaDePago;
                    //tdAModificar.Importe = itemDTO.importe;
                    tdAModificar.IdUsuario = itemDTO.idUsuario;
                    tdAModificar.ModifiedDate = DateTime.Now;

                    await _context.SaveChangesAsync();

                    dbContextTransaction.Commit();
                    return new { result = "ok" };

                }
                catch (Exception ex)
                {
                    dbContextTransaction.Rollback();
                    return new { result = "error", message = "Ocurrio un error. Exception: " + ex.Message };
                }
            }
      
        }
        [NonAction]
        [HttpDelete]
        public async Task<ActionResult<object>> Delete(ItemMovimientoDeCajaDeleteRequest itemDTO)
        {
            try
            {
                Gasto? tdAModificar = await _context.Gasto.FindAsync(itemDTO.id);
                if (tdAModificar == null)
                {
                    return new { result = "error", message = "Item inexistente Id: " + itemDTO.id.ToString() };
                }
                //tdAModificar.Activo = "N";
                tdAModificar.IdUsuario = itemDTO.idUsuario;
                tdAModificar.ModifiedDate = DateTime.Now;

                await _context.SaveChangesAsync();

                return new { result = "ok" };

            }
            catch (Exception ex)
            {
                return new { result = "error", message = "Ocurrio un error. Exception: " + ex.Message };
            }
        }


    }

}

