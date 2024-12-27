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
using WebApiMariaMC.Servicies;
using System.ServiceModel.Channels;
using WebApiMariaMC.AFIP.Entities;
using Entities.ResponseModels;

namespace WebApiMariaMC.Controllers
{

    [Route("api/[controller]/[action]")]
    [ApiController]
    public class VentaController : ControllerBase
    {
        private readonly IVentaService _ventaService;
        private readonly AfipService _afipService;
        public VentaController(IVentaService ventaService, AfipService afipService)
        {
            _ventaService = ventaService;
            _afipService = afipService;
        }

        [HttpPost(Name = "RealizarVenta")]
        public async Task<ActionResult<object>> RealizarVenta(VentaRequest venta)
        {

            long idVenta = await _ventaService.RealizarVenta(venta);
            if(idVenta > 0)
            {
                return new { result = "ok", message = "La venta se realizo exitosamente nro: " + idVenta.ToString(), idVenta };
            }
            else 
            {
                return new { result = "error", message = "Ocurrio un error, la venta NO se realizo - nro: " + idVenta.ToString(), idVenta = -1};
            }
            
        }

        [HttpGet(Name = "GetAllVentasPag")]
        public async Task<ActionResult<object>> GetAllVentasPaginado(int idSucursal,string fechaDesde,string fechaHasta,
            int pageNumber = 1,int pageSize = 10)
        {
            try
            {
                if (string.IsNullOrEmpty(fechaDesde) || string.IsNullOrEmpty(fechaHasta))
                {
                    return new { result = "error", message = "Ocurrio un error, al listar las ventas, fechas vacias." };
                }
                // Convertir las fechas de string a DateTime si no son nulas
                DateTime fechaDesdeParsed = DateTime.ParseExact(fechaDesde, "dd/MM/yyyy", null);
                DateTime fechaHastaParsed = DateTime.ParseExact(fechaHasta, "dd/MM/yyyy", null);                

                // Llamar al servicio pasando las fechas convertidas
                ListadoVentasResponse response = await _ventaService.GetAllVentasPaginado(idSucursal, fechaDesdeParsed, fechaHastaParsed, pageNumber, pageSize);

                if (response.ventas.Count > 0)
                {
                    return new { result = "ok", response.ventas, response.totalDeRegistros };
                }
                else if (response.ventas.Count == 0)
                {
                    return new { result = "ok", response.ventas, response.totalDeRegistros, message = "No Hay ventas para mostrar" };
                }
                else
                {
                    return new { result = "error", message = "Ocurrio un error, al listar las ventas" };
                }
            }
            catch (FormatException)
            {
                // En caso de que el formato de la fecha sea incorrecto
                return new { result = "error", message = "El formato de las fechas es inválido. Use 'dd/MM/yyyy'." };
            }
            catch (Exception ex)
            {
                // Manejo general de errores
                return new { result = "error", message = $"Ocurrio un error: {ex.Message}" };
            }
        }


        [HttpPost(Name = "EnviarMailVenta")]
        public async Task<ActionResult<object>> EnviarMailVenta(GenerarComprobanteRequest comprobanteRequet)
        {
            return await _afipService.GenerarComprobanteYEnviarAlCliente(comprobanteRequet.idVenta, comprobanteRequet.idUsuario);

        }
        // Endpoint para descargar la factura en PDF
        [HttpGet("descargar-factura/{idComprobante}")]
        public async Task<IActionResult> DescargarFactura(int idComprobante)
        {
            byte[] pdfBytes = await _afipService.generarFactura(idComprobante);
            return File(pdfBytes, "application/pdf", $"Comprobante_{idComprobante}.pdf");

        }      
        [HttpPost(Name = "consultarUltimoComprobanteAfip")]
        public async Task<ActionResult<object>> consultarUltimoComprobanteAfip(int idSucursal)
        {
            var xml = await _afipService.FECompConsultarAsync(idSucursal);
            return new { result = "ok", message = xml };
        } 
        [HttpPost(Name = "UltimoNroComprobanteRegistradoAfip")]
        public async Task<ActionResult<object>> UltimoNroComprobanteRegistradoAfip(int idSucursal)
        {
            var xml = await _afipService.FECompUltimoAutorizadoAsync(idSucursal);
            return new { result = "ok", message = xml };
        }

        [HttpPost(Name = "statusFEAfip")]
        public async Task<ActionResult<object>> statusFEAfip()
        {

            string response = await _afipService.FEDummyAsync();
            return new { result = "ok", message = response };

        }
        [HttpPost(Name = "statusPadronAfip")]
        public async Task<ActionResult<object>> statusPadronAfip()
        {

            object response = await _afipService.PersonaDummyAsync();
            return new { result = "ok", response };

        }

        [HttpPost(Name = "getTiposComprobantesAfip")]
        public async Task<ActionResult<object>> getTiposComprobantesAfip(int idSucursal)
        {
            var lista = await _afipService.FEParametrosAfipAsync("FEParamGetTiposCbte", idSucursal);
            return new { result = "ok", parametros = lista };
        }

        [HttpPost(Name = "getTiposConceptosAfip")]
        public async Task<ActionResult<object>> getTiposConceptosAfip(int idSucursal)
        {
            var lista = await _afipService.FEParametrosAfipAsync("FEParamGetTiposConcepto", idSucursal);
            return new { result = "ok", parametros = lista };
        }

        [HttpPost(Name = "getTiposDocumentosAfip")]
        public async Task<ActionResult<object>> getTiposDocumentosAfip(int idSucursal)
        {
            var lista = await _afipService.FEParametrosAfipAsync("FEParamGetTiposDoc", idSucursal);
            return new { result = "ok", parametros = lista };
        }        
        
        [HttpPost(Name = "getTiposIvaAfip")]
        public async Task<ActionResult<object>> getTiposIvaAfip(int idSucursal)
        {
            var lista = await _afipService.FEParametrosAfipAsync("FEParamGetTiposIva", idSucursal);
            return new { result = "ok", parametros = lista };
        }     
        [HttpPost(Name = "getTiposTributosAfip")]
        public async Task<ActionResult<object>> getTiposTributosAfip(int idSucursal)
        {
            var lista = await _afipService.FEParametrosAfipAsync("FEParamGetTiposTributos", idSucursal);
            return new { result = "ok", parametros = lista };
        }
        [HttpPost(Name = "getTiposMonedasAfip")]
        public async Task<ActionResult<object>> getTiposMonedasAfip(int idSucursal)
        {
            var lista = await _afipService.FEParamGetTiposMonedasAsync(idSucursal);
            return new { result = "ok", parametros = lista };
        }
        [HttpPost(Name = "getTiposOpcionalesAfip")]
        public async Task<ActionResult<object>> getTiposOpcionalesAfip(int idSucursal)
        {
            var lista = await _afipService.FEParamGetTiposOpcionalAsync(idSucursal);
            return new { result = "ok", parametros = lista };
        }

        [HttpPost(Name = "getPuntosDeVentaAfip")]
        public async Task<ActionResult<object>> getPuntosDeVentaAfip(int idSucursal)
        {
            var lista = await _afipService.FEParamGetPtosVentaAsync(idSucursal);
            return new { result = "ok", parametros = lista };
        }

        [HttpPost(Name = "getIdPersonaListByDocumentoAfip")]
        public async Task<ActionResult<object>> getIdPersonaListByDocumentoAfip(int idSucursal, long nroDocumento)
        {
            try
            {
                var datos = await _afipService.getIdPersonaListByDocumento(idSucursal, nroDocumento);

                if (datos == null || datos.Count ==0)
                    return new { result = "error", message = $"El nro de documento no es valido o esta inactivo ({nroDocumento})" };

                List<PersonaResponse> personas = new List<PersonaResponse>();
                foreach (var dato in datos) {
                    var persona = await _afipService.getIdPersona(idSucursal, Convert.ToInt64(dato));
                    if (persona != null && persona.Persona.IdPersona != 0)
                        personas.Add(persona);
                }

                return new { result = "ok", personas };
            }
            catch {
                return new { result = "error", message = $"Ocurrio un error, al obtener los datos del nro: {nroDocumento}" };
            }


        }
        [HttpPost(Name = "getIdPersonaAfip")]
        public async Task<ActionResult<object>> getIdPersonaListAfip(int idSucursal,long cuit)
        {
            var datos = await _afipService.getIdPersona(idSucursal,cuit);


            return new { result = "ok", Datos = datos };
        }
    }

}

