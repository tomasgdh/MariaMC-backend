using Microsoft.AspNetCore.Mvc;
using Entities.Items;
using WebApiMariaMC.IServicies;
using Entities.RequestModels;

namespace WebApiMariaMC.Controllers
{

    [Route("api/[controller]/[action]")]
    [ApiController]
    public class ProductoController : ControllerBase
    {
        private readonly IProductoService _productoService;
        public ProductoController(IProductoService productoService)
        {
            _productoService = productoService;
        }

        // GET: api/GetProducto
        [HttpGet("{codigo}")]
        public async Task<ActionResult<object>> GetProductoToSell(string codigo)
        {
            return await _productoService.GetProductoToSell(codigo);
        }

        // GET: api/GetProducto
        [HttpGet("{codigo}")]
        public async Task<ActionResult<object>> GetProducto(string codigo)
        {
            return await _productoService.GetProducto(codigo);
        }

        [HttpPost(Name = "CargaDeProductosPorArchivo")]
        public async Task<ActionResult<object>> CargaDeProductosPorArchivo([FromBody] CargarProductosRequest request)
        {
            return await _productoService.CargaDeProductosporArchivo(request);
        }

        [HttpPost(Name = "GuardarProducto")]
        public async Task<ActionResult<object>> GuardarProducto([FromBody] UpdateProductosRequest request)
        {
            return await _productoService.GuardarProducto(request);
        }

        [HttpGet(Name = "GetAllProductosToPrint")]
        public async Task<ActionResult<object>> GetAllProductosToPrint()
        {
            return await _productoService.GetAllProductosToPrint();
        }


        // POST: api/
        //[NonAction]
        //[HttpPost(Name = "AddProducto")]
        //public async Task<ActionResult<Producto>> Add(Producto tipo)
        //{
        //    _context.Productos.Add(tipo);
        //    await _context.SaveChangesAsync();

        //    return CreatedAtAction("GetProducto", new { id = tipo.IdProducto }, tipo);
        //}


        //DELETE: api/Producto/5
        //[NonAction]
        //[HttpDelete("{id}")]
        //public async Task<IActionResult> Delete(int id)
        //{
        //    var tipo = await _context.Productos.FindAsync(id);
        //    if (tipo == null)
        //    {
        //        return NotFound();
        //    }

        //    _context.Productos.Remove(tipo);
        //    await _context.SaveChangesAsync();

        //    return NoContent();
        //}

        //private bool Exists(long id)
        //{
        //    return _context.Productos.Any(e => e.IdProducto == id);
        //}
    }

}

