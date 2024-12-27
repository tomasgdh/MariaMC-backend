using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Data.Models;
using Entities.Items;
using Enumeradores;
using Entities.RequestModels;

namespace WebApiMariaMC.Controllers
{

    [Route("api/[controller]/[action]")]
    [ApiController]
    public class TipoFormaDePagoController : ControllerBase
    {
        private readonly Maria_MCContext _context;  // Ajusta esto con el nombre correcto de tu DbContext

        public TipoFormaDePagoController(Maria_MCContext context)
        {
            _context = context;
        }

        // GET: api/TiposDocumento
        [HttpGet(Name = "GetAllFormasDePago")]
        public async Task<ActionResult<IEnumerable<object>>> GetAllFormasDePago()
        {
            var lista = await _context.TipoFormaDePago
             .Where(td => td.Activo == "S")
             .Select(td => new { id = td.Id, description = td.Descripcion,descuento = td.Descuento })
             .OrderBy(td => td.description) // Ordena alfabéticamente por descripción
             .ToListAsync();

            // Crea el elemento "Seleccione una Marca"
            var selectItem = new { id = 0, description = "Seleccione una Forma de Pago",descuento = (decimal?)null};

            // Añade el elemento al inicio de la lista
            lista.Insert(0, selectItem);

            return lista;



        }

        [HttpGet(Name = "GetAllFormasDePagoCompras")]
        public async Task<ActionResult<IEnumerable<object>>> GetAllFormasDeCompra()
        {
            var lista = await _context.TipoFormaDePago
                                 .Where(td => td.Activo == "S" && ( td.Id == (int)TipoFormaPago.EFECTIVO ||
                                                                    td.Id == (int)TipoFormaPago.MP_TRANSFERENCIA))
                                 .Select(td => new { id = td.Id, description = td.Descripcion})
                                 .OrderBy(td => td.description) // Ordena alfabéticamente por descripción
                                 .ToListAsync();

            // Crea el elemento "Seleccione una Marca"
            var selectItem = new { id = 0, description = "Seleccione una Forma de Pago" };

            // Añade el elemento al inicio de la lista
            lista.Insert(0, selectItem);

            return lista;
        }

        [HttpGet]
        public async Task<ActionResult<object>> GetAll()
        {
            try
            {
                var lista = await _context.TipoFormaDePago
                                   .Select(td => new { id = td.Id, descripcion = td.Descripcion, activo = td.Activo, descuento = td.Descuento ?? 0 })
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
        [NonAction]
        [HttpGet(Name = "GetTipoFormaDePago")]
        public async Task<ActionResult<object>> Get(int id)
        {
            var tipo = await _context.TipoFormaDePago.FindAsync(id);

            if (tipo == null)
            {
                return new { result = "error", message = "Item inexistente Id: " + id.ToString() };
            }

            return new { result = "ok", item = tipo };
        }

        [HttpPost]
        public async Task<ActionResult<object>> Add(ItemTBTipoFormaDePagoRequest itemDTO)
        {
            try
            {
                TipoFormaDePago item = new TipoFormaDePago
                {
                    Descripcion = itemDTO?.descripcion,
                    Descuento = itemDTO?.descuento,
                    Activo = itemDTO?.activo,
                    CreatedDate = DateTime.Now,
                    IdUsuario = itemDTO.idUsuario,
                };
                _context.TipoFormaDePago.Add(item);
                await _context.SaveChangesAsync();

                return new { result = "ok", item };
            }
            catch (Exception ex)
            {
                return new { result = "error", message = "Ocurrio un error. Exception: " + ex.Message };
            }
        }

        [HttpPost]
        public async Task<ActionResult<object>> Update(ItemTBTipoFormaDePagoRequest itemDTO)
        {
            try
            {
                TipoFormaDePago? tdAModificar = await _context.TipoFormaDePago.FindAsync(itemDTO.id);
                if (tdAModificar == null)
                {
                    return new { result = "error", message = "Item inexistente Id: " + itemDTO.id.ToString() };
                }
                tdAModificar.Descripcion = itemDTO.descripcion;
                tdAModificar.Activo = itemDTO.activo;
                tdAModificar.Descuento = itemDTO?.descuento == 0 ? null : itemDTO?.descuento;
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

        [HttpDelete]
        public async Task<ActionResult<object>> Delete(ItemTablabasicaDeleteRequest itemDTO)
        {
            try
            {
                TipoFormaDePago? tdAModificar = await _context.TipoFormaDePago.FindAsync(itemDTO.id);
                if (tdAModificar == null)
                {
                    return new { result = "error", message = "Item inexistente Id: " + itemDTO.id.ToString() };
                }
                tdAModificar.Activo = "N";
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

