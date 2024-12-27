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
using Entities.ResponseModels;

namespace WebApiMariaMC.Controllers
{

    [Route("api/[controller]/[action]")]
    [ApiController]
    public class CierreDeCajaController : ControllerBase
    {
        private readonly ICierreDeCajaService _cierreDeCajaService;
        public CierreDeCajaController(ICierreDeCajaService cierreDeCajaService)
        {
            _cierreDeCajaService = cierreDeCajaService;
        }

        [HttpPost(Name = "RealizarCierreDeCaja")]
        public async Task<ActionResult<object>> RealizarCierreDeCaja(CierreDeCajaRequest cierreDeCaja)
        {

            CierreDeCajaResponse cierre = await _cierreDeCajaService.CierreDeCaja(cierreDeCaja);
            if(cierre.IdCierre > 0)
            {
                return new { result = "ok", message = "El cierre de caja se realizo exitosamente nro: " + cierre.IdCierre.ToString(), idCierreDeCaja = cierre.IdCierre, cierre };
            }
            else if(cierre.IdCierre == -2)
            {
                return new { result = "error", message = "No hay movimientos para realizar el cierre", idCierreDeCaja = -1 };
            }
            else 
            {
                return new { result = "error", message = "Ocurrio un error, la cierreDeCaja NO se realizo - nro: " + cierre.IdCierre.ToString(), idCierreDeCaja = -1};
            }
        }

        [HttpPost(Name = "RealizarCierreDeCajaX")]
        public async Task<ActionResult<object>> RealizarCierreDeCajaX(CierreDeCajaRequest cierreDeCaja)
        {

            CierreDeCajaResponse cierre = await _cierreDeCajaService.CierreDeCajaX(cierreDeCaja);
            if (cierre.IdCierre == 0)
            {
                return new { result = "ok", message = "El cierre de caja X se realizo exitosamente",  cierre };
            }
            else if (cierre.IdCierre == -2)
            {
                return new { result = "error", message = "No hay movimientos para realizar el cierre", idCierreDeCaja = -1 };
            }
            else
            {
                return new { result = "error", message = "Ocurrio un error, la cierreDeCaja X NO se realizo - nro: " + cierre.IdCierre.ToString(), idCierreDeCaja = -1 };
            }
        }

        [HttpGet(Name = "GetCierreDeCaja")]
        public async Task<ActionResult<object>> GetCierreDeCaja(int idCierre)
        {

            CierreDeCajaResponse cierre = await _cierreDeCajaService.GetCierreDeCaja(idCierre);
            if (cierre.IdCierre > 0)
            {
                return new { result = "ok", cierre };
            }
            else
            {
                return new { result = "error", message = "Ocurrio un error, el cierreDeCaja NO existe - nro: " + idCierre.ToString() };
            }
        }

        [HttpGet(Name = "GetUltimoCierreDeCaja")]
        public async Task<ActionResult<object>> GetUltimoCierreDeCaja()
        {

            CierreDeCajaResponse cierre = await _cierreDeCajaService.GetUltimoCierreDeCaja();
            if (cierre.IdCierre > 0)
            {
                return new { result = "ok", cierre };
            }
            else
            {
                return new { result = "error", message = "Ocurrio un error, el cierreDeCaja NO existe."};
            }
        }

        [HttpGet(Name = "GetAllCierreDeCaja")]
        public async Task<ActionResult<object>> GetAllCierreDeCaja(int idSucursal)
        {

            List<CCResponseDetalle> cierres = await _cierreDeCajaService.GetAllCierreDeCaja(idSucursal);
            if (cierres.Count > 0)
            {
                return new { result = "ok", cierres };
            }
            else if (cierres.Count == 0)
            {
                return new { result = "error", message =  "No Hay cierres de caja para mostrar" };
            }
            else
            {
                return new { result = "error", message = "Ocurrio un error, el cierreDeCaja NO existe." };
            }
        }

        [HttpGet(Name = "GetAllCierreDeCajaPag")]
        public async Task<ActionResult<object>> GetAllCierreDeCajaPaginado(int idSucursal, int pageNumber = 1, int pageSize = 10)
        {

            CCResponse response = await _cierreDeCajaService.GetAllCierreDeCajaPaginado(idSucursal, pageNumber,pageSize);
            if (response.Cierres.Count > 0)
            {
                return new { result = "ok", cierres = response.Cierres, response.totalDeRegistros};
            }
            else if (response.Cierres.Count == 0)
            {
                return new { result = "error", message = "No Hay cierres de caja para mostrar" };
            }
            else
            {
                return new { result = "error", message = "Ocurrio un error, el cierreDeCaja NO existe." };
            }
        }

    }

}

