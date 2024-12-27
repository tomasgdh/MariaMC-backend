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
    public class TipoGastoController : ControllerBase
    {
        private readonly Maria_MCContext _context;  // Ajusta esto con el nombre correcto de tu DbContext

        public TipoGastoController(Maria_MCContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<object>> GetAllActivos()
        {
            try
            {
                var lista = await _context.TipoGasto
                            .Where(td => td.Activo == "S")
                            .Select(td => new { id = td.Id, description = td.Descripcion })
                            .OrderBy(td => td.description) // Ordena alfabéticamente por descripción
                            .ToListAsync();

                if (lista.Count == 0)
                {
                    return new { result = "error", message = "No hay datos para mostrar" };
                }

                var selectItem = new { id = 0, description = "Seleccione un Tipo de Gasto" };

                // Añade el elemento al inicio de la lista
                lista.Insert(0, selectItem);

                return new { result = "ok", lista };
            }
            catch (Exception ex)
            {

                return new { result = "error", message = "Ocurrio un error. Exception: " + ex.Message };

            }
        }

        [HttpGet]
        public async Task<ActionResult<object>> GetAll()
        {
            try {
                var lista = await _context.TipoGasto
                                   .Select(td => new { id = td.Id, descripcion = td.Descripcion, activo = td.Activo })
                                   .ToListAsync();
                if (lista.Count == 0)
                {
                    return new { result = "error", message = "No hay datos para mostrar" };
                }

                return new { result = "ok", lista };
            }
            catch (Exception ex) {

                return new { result = "error", message = "Ocurrio un error. Exception: "  + ex.Message};

            }
        }
        [NonAction]
        [HttpGet(Name = "GetTipoGasto")]
        public async Task<ActionResult<object>> Get(int id)
        {
            var tipo = await _context.TipoGasto.FindAsync(id);

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
                TipoGasto item = new TipoGasto
                {
                    Descripcion = itemDTO?.descripcion,
                    Activo = itemDTO?.activo,
                    CreatedDate = DateTime.Now,
                    IdUsuario = itemDTO.idUsuario,
                };
                _context.TipoGasto.Add(item);
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
                TipoGasto? tdAModificar = await _context.TipoGasto.FindAsync(itemDTO.id);
                if (tdAModificar == null)
                {
                    return new { result = "error", message = "Item inexistente Id: " + itemDTO.id.ToString() };
                }
                tdAModificar.Descripcion = itemDTO.descripcion;
                tdAModificar.Activo = itemDTO.activo;
                tdAModificar.IdUsuario = itemDTO.idUsuario;
                tdAModificar.ModifiedDate = DateTime.Now;
                
                await _context.SaveChangesAsync();

                return new { result = "ok"};

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
                TipoGasto? tdAModificar = await _context.TipoGasto.FindAsync(itemDTO.id);
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

