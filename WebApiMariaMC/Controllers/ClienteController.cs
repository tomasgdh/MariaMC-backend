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
    public class ClienteController : ControllerBase
    {
        private readonly Maria_MCContext _context;  // Ajusta esto con el nombre correcto de tu DbContext

        public ClienteController(Maria_MCContext context)
        {
            _context = context;
        }

        // GET: api/TiposDocumento
        [NonAction]
        [HttpGet(Name = "GetAllClientes")]
        public async Task<ActionResult<IEnumerable<object>>> GetAll()
        {
            return await _context.Clientes.Select(td => new { id = td.IdCliente,
                                                              description = td.Apellido+", "+td.Nombre,
                                                              usuario = td.Mail,
                                                              nroDocuemnto = td.NroDocumento,
                                                              creditoEnTienda = td.SaldoEnCuenta})
                                           .ToListAsync();
        }

        // GET: api/GetProducto
        [HttpGet("{busqueda}")]
        public async Task<ActionResult<object>> GetCliente(string busqueda)
        {
            var clientes = await _context.Clientes
                .Where(c => EF.Functions.Like(c.Apellido.ToLower(), $"%{busqueda.ToLower()}%") ||
                            EF.Functions.Like(c.Nombre.ToLower(), $"%{busqueda.ToLower()}%") ||
                            EF.Functions.Like(c.NroDocumento.ToLower(), $"%{busqueda.ToLower()}%") ||
                            EF.Functions.Like(c.Mail.ToLower(), $"%{busqueda.ToLower()}%"))
                .Select(td => new {
                    id = td.IdCliente,
                    description = td.Apellido + ", " + td.Nombre,
                    usuario = td.Mail,
                    nroDocumento = td.NroDocumento,
                    creditoEnTienda = td.SaldoEnCuenta
                })
                .ToListAsync();

            if (clientes == null || clientes.Count == 0)
            {
                return new { result = "error", message = "Cliente inexistente" };
            }


            return new { result = "ok", clientes };
        }

        // GET: api/TiposDocumento/5
        [NonAction]
        [HttpGet(Name ="GetCliente")]
        public async Task<ActionResult<Cliente>> Get(string id)
        {
            var tipo = await _context.Clientes.FindAsync(id);

            if (tipo == null)
            {
                return NotFound();
            }

            return tipo;
        }

        // POST: api/TiposDocumento
        [HttpPost(Name = "GuardarCliente")]
        public async Task<ActionResult<object>> Add(ClienteRequest cli)
        {
            Cliente cliente = new Cliente();
            cliente.Nombre = cli.nombre.ToUpper();
            cliente.Apellido = cli.apellido.ToUpper();
            cliente.Mail = cli.mail.ToLower();
            cliente.NroDocumento = cli.nroDocumento.ToString();
            cliente.IdTipoDocumento = cli.idTipoDocumento;
            cliente.CreatedDate = DateTime.Now;
            cliente.IdUsuario = cli.idUsuario;

            if (ExistsMail(cliente.Mail))
            {
                return new { result = "error", message = "Ocurrio un error, el mail del cliente ya esta asociado a otra cuenta", IdCliente = -1 };
            }

            if (ExistsDNI(cliente.NroDocumento,cliente.IdTipoDocumento))
            {
                return new { result = "error", message = "Ocurrio un error, el Nro Documento del cliente ya Existe", IdCliente = -1 };
            }

            _context.Clientes.Add(cliente);
            await _context.SaveChangesAsync();

            if (cliente.IdCliente > 0)
            {
                return new { result = "ok", message = "El cliente se guardo exitosamente nro: " + cliente.IdCliente.ToString(), cliente.IdCliente };
            }
            else
            {
                return new { result = "error", message = "Ocurrio un error, el cliente no se guardo", IdCliente = -1 };
            }

        }
        // PUT: api/TiposDocumento/5
        [NonAction]
        [HttpPut(Name = "UpdateCliente")]
        public async Task<IActionResult> Update(Cliente cliente)
        {
            var tdAModificar = await _context.Clientes.FindAsync(cliente.IdCliente);
            if (tdAModificar == null)
            {
                return NotFound();
            }
            tdAModificar.Nombre = cliente.Nombre;
            tdAModificar.Apellido = cliente.Apellido;
            tdAModificar.NroDocumento = cliente.NroDocumento;
            tdAModificar.IdTipoDocumento = cliente.IdTipoDocumento;
            tdAModificar.Mail = cliente.Mail;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!Exists(cliente.IdCliente))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/TipoTalle/5
        [NonAction]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var cliente = await _context.Clientes.FindAsync(id);
            if (cliente == null)
            {
                return NotFound();
            }

            _context.Clientes.Remove(cliente);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool Exists(int id)
        {
            return _context.Clientes.Any(e => e.IdCliente == id);
        }
        private bool ExistsDNI(string nroDocu, int idtipoDocu)
        {
            return _context.Clientes.Any(e => e.IdTipoDocumento == idtipoDocu && e.NroDocumento == nroDocu);
        }
        private bool ExistsMail(string mail)
        {
            return _context.Clientes.Any(e => e.Mail == mail.ToLower());
        }
    }

}

