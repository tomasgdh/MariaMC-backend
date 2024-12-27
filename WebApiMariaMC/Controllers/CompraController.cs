using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Data.Models;
using Entities.Items;
using Enumeradores;
using Entities.RequestModels;
using WebApiMariaMC.IServicies;

namespace WebApiMariaMC.Controllers
{

    [Route("api/[controller]/[action]")]
    [ApiController]
    public class CompraController : ControllerBase
    {
        private readonly ICompraService _compraService;
        public CompraController(ICompraService compraService)
        {
            _compraService = compraService;
        }

        [HttpPost(Name = "RealizarCompra")]
        public async Task<ActionResult<object>> RealizarCompra(CompraRequest compra)
        {

            long idCompra = await _compraService.RealizarCompra(compra);
            if(idCompra > 0)
            {
                return new { result = "ok", message = "La compra de lote se realizo exitosamente nro: " + idCompra.ToString(), idCompra };
            }
            else 
            {
                return new { result = "error", message = "Ocurrio un error, la compra de lote NO se realizo - nro: " + idCompra.ToString(), idCompra = -1};
            }
        }

    }

}

