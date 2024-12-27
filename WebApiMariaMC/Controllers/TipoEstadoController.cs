using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Data.Models;
using Entities.Items;
using Entities.RequestModels;

namespace WebApiMariaMC.Controllers
{

    [Route("api/[controller]/[action]")]
    [ApiController]
    public class TipoEstadoController : ControllerBase
    {
        private readonly Maria_MCContext _context;  // Ajusta esto con el nombre correcto de tu DbContext

        public TipoEstadoController(Maria_MCContext context)
        {
            _context = context;
        }

        // GET: api/TiposDocumento
        [HttpGet(Name = "GetAllEstados")]
        public async Task<ActionResult<IEnumerable<object>>> GetAllEstados()
        {
            return await _context.TipoEstado
                                .Where(td => td.Activo == "S")
                                .Select(td => new { id = td.Id, description = td.Descripcion })
                                .OrderBy(td => td.id == 0 ? 0 : 1) // Pone primero los que tienen id = 0
                                .ThenBy(td => td.description) // Luego ordena alfabéticamente por descripción
                                .ToListAsync();
        }

        [HttpGet]
        public async Task<ActionResult<object>> GetAll()
        {
            try
            {
                var lista = await _context.TipoEstado
                                   .Select(td => new { id = td.Id, descripcion = td.Descripcion, activo = td.Activo })
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
        [HttpGet(Name = "GetTipoEstado")]
        public async Task<ActionResult<object>> Get(int id)
        {
            var tipo = await _context.TipoEstado.FindAsync(id);

            if (tipo == null)
            {
                return new { result = "error", message = "Item inexistente Id: " + id.ToString() };
            }

            return new { result = "ok", item = tipo };
        }

        [HttpPost]
        public async Task<ActionResult<object>> Add(ItemTablabasicaRequest itemDTO)
        {
            try
            {
                TipoEstado item = new TipoEstado
                {
                    Descripcion = itemDTO?.descripcion,
                    Activo = itemDTO?.activo,
                    CreatedDate = DateTime.Now,
                    IdUsuario = itemDTO.idUsuario,
                };
                _context.TipoEstado.Add(item);
                await _context.SaveChangesAsync();

                return new { result = "ok", item };
            }
            catch (Exception ex)
            {
                return new { result = "error", message = "Ocurrio un error. Exception: " + ex.Message };
            }
        }

        [HttpPost]
        public async Task<ActionResult<object>> Update(ItemTablabasicaRequest itemDTO)
        {
            try
            {
                TipoEstado? tdAModificar = await _context.TipoEstado.FindAsync(itemDTO.id);
                if (tdAModificar == null)
                {
                    return new { result = "error", message = "Item inexistente Id: " + itemDTO.id.ToString() };
                }
                tdAModificar.Descripcion = itemDTO.descripcion;
                tdAModificar.Activo = itemDTO.activo;
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
                TipoEstado? tdAModificar = await _context.TipoEstado.FindAsync(itemDTO.id);
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

