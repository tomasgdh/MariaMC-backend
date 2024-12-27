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
    public class TipoTalleController : ControllerBase
    {
        private readonly Maria_MCContext _context;  // Ajusta esto con el nombre correcto de tu DbContext

        public TipoTalleController(Maria_MCContext context)
        {
            _context = context;
        }

        // GET: api/TiposDocumento

        [HttpGet(Name = "GetAllTalles")]
        public async Task<ActionResult<IEnumerable<object>>> GetAllTalles()
        {
            var lista = await _context.TipoTalle
                         .Where(td => td.Activo == "S")
                         .Select(td => new { id = td.Id, description = td.Descripcion, td.categoria})
                         .OrderBy(td => td.description) // Ordena alfabéticamente por descripción
                         .ToListAsync();

            // Crea el elemento "Seleccione una Marca"
            var selectItem = new { id = 0, description = "Seleccione un Talle", categoria = "" };

            // Añade el elemento al inicio de la lista
            lista.Insert(0, selectItem);

            return lista;
        }
        [HttpGet]
        public async Task<ActionResult<object>> GetAll()
        {
            try
            {
                var lista = await _context.TipoTalle
                                   .Select(td => new { id = td.Id, descripcion = td.Descripcion, td.categoria, activo = td.Activo })
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
        [HttpGet(Name = "GetTipoTalle")]
        public async Task<ActionResult<object>> Get(int id)
        {
            var tipo = await _context.TipoTalle.FindAsync(id);

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
                TipoTalle item = new TipoTalle
                {
                    Descripcion = itemDTO?.descripcion,
                    Activo = itemDTO?.activo,
                    CreatedDate = DateTime.Now,
                    IdUsuario = itemDTO.idUsuario,
                };
                _context.TipoTalle.Add(item);
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
                TipoTalle? tdAModificar = await _context.TipoTalle.FindAsync(itemDTO.id);
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
                TipoTalle? tdAModificar = await _context.TipoTalle.FindAsync(itemDTO.id);
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

