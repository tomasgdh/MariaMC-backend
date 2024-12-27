using Data.Models;
using Entities.Items;
using Logic.Logic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.IIS.Core;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using WebApiMariaMC.AFIP;
using WebApiMariaMC.AFIP.Entities;
using DinkToPdf;

namespace WebApiMariaMC.Servicies
{
    public class AfipService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly Maria_MCContext _context;  // Ajusta esto con el nombre correcto de tu DbContext

        private readonly LoginTicketRequestGenerator _requestGenerator;
        private readonly CmsGenerator _cmsGenerator;

        public AfipService(IConfiguration configuration, HttpClient httpClient, Maria_MCContext context)
        {
            _configuration = configuration;
            _httpClient = httpClient;
            _context = context;
            _requestGenerator = new LoginTicketRequestGenerator();
            _cmsGenerator = new CmsGenerator();
        }

        public async Task<ParamAfip> AuthenticateAsync(int IdSucursal, string servicio = "wsfe")
        {
            try {

                var datosAfip = await _context.ParamAfip.FirstAsync(v => v.IdSucursal == IdSucursal);

                if (datosAfip == null)
                    throw new Exception("Error al obtener datos de Afip de la sucursal!");

                DateTime fechaExpiracion = datosAfip.FechaExpiracion;
                DateTime ahora = DateTime.Now;

                // Calcula la diferencia entre la fecha de expiración y el momento actual
                TimeSpan diferencia = ahora - fechaExpiracion;

                // Verifica si la diferencia es mayor a tres horas
                if (diferencia.TotalHours < 3)
                {
                    return datosAfip;
                }
                // Obtener la ruta desde el archivo de configuración
                string rutaCertRelativa = _configuration["Afip:certificadoMariaMC"];
                string rutaKeyRelativa = _configuration["Afip:keyMariaMC"];

                if (rutaCertRelativa == null)
                    throw new Exception("Error al obtener la ruta relativa del Certificado!");

                if (rutaKeyRelativa == null)
                    throw new Exception("Error al obtener datos la ruta relativa de la Key");

                var loginTicketRequestXml = _requestGenerator.GenerateLoginTicketRequest(servicio);

                var cms = _cmsGenerator.GenerateSignedCms(
                    loginTicketRequestXml,
                    rutaCertRelativa,  // Ruta al certificado PFX
                    rutaKeyRelativa   // Ruta a la clave privada si es necesario                    
                );

                string authResponse = await CallLoginCmsAsync(cms);

                var authData = ParseLoginCmsResponse(authResponse);


                if (!String.IsNullOrEmpty(authData.Token)  && !String.IsNullOrEmpty(authData.Sign))
                {
                    //guardar token y firma.
                    datosAfip.FechaExpiracion = DateTime.Now;
                    datosAfip.Token = authData.Token;
                    datosAfip.Sign = authData.Sign;

                    _context.SaveChanges();

                    return datosAfip;
                }
                else
                {
                    throw new Exception("Error al obtener datos de Afip de la sucursal!");
                }

            } catch (Exception e){
            
            var message = e.Message;
                throw new Exception("Error al Authenticar");
            }   
           
        }

        public async Task<ParamAfip> AuthenticatePadronAsync(int IdSucursal, string servicio = "ws_sr_padron_a13")
        {
            try
            {

                var datosAfip = await _context.ParamAfip.FirstAsync(v => v.IdSucursal == IdSucursal);

                if (datosAfip == null)
                    throw new Exception("Error al obtener datos de Afip de la sucursal!");

                DateTime fechaExpiracion = datosAfip.FechaExpiracionPadron;
                DateTime ahora = DateTime.Now;

                // Calcula la diferencia entre la fecha de expiración y el momento actual
                TimeSpan diferencia = ahora - fechaExpiracion;

                // Verifica si la diferencia es mayor a tres horas
                if (diferencia.TotalHours < 3)
                {
                    return datosAfip;
                }
                // Obtener la ruta desde el archivo de configuración
                string rutaCertRelativa = _configuration["Afip:certificadoMariaMC"];
                string rutaKeyRelativa = _configuration["Afip:keyMariaMC"];

                if (rutaCertRelativa == null)
                    throw new Exception("Error al obtener la ruta relativa del Certificado!");

                if (rutaKeyRelativa == null)
                    throw new Exception("Error al obtener datos la ruta relativa de la Key");

                var loginTicketRequestXml = _requestGenerator.GenerateLoginTicketRequest(servicio);

                var cms = _cmsGenerator.GenerateSignedCms(
                    loginTicketRequestXml,
                    rutaCertRelativa,  // Ruta al certificado PFX
                    rutaKeyRelativa   // Ruta a la clave privada si es necesario                    
                );

                string authResponse = await CallLoginCmsAsync(cms);

                var authData = ParseLoginCmsResponse(authResponse);


                if (!String.IsNullOrEmpty(authData.Token) && !String.IsNullOrEmpty(authData.Sign))
                {
                    //guardar token y firma.
                    datosAfip.FechaExpiracionPadron = DateTime.Now;
                    datosAfip.TokenPadron = authData.Token;
                    datosAfip.SignPadron = authData.Sign;

                    _context.SaveChanges();

                    return datosAfip;
                }
                else
                {
                    throw new Exception("Error al obtener datos de Afip de la sucursal!");
                }

            }
            catch (Exception e)
            {

                var message = e.Message;
                throw new Exception("Error al Authenticar");
            }

        }

        public async Task<string> CallLoginCmsAsync(string cms)
        {
            var soapRequest = $@"
            <soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:wsaa=""http://wsaa.view.sua.dvadac.desein.afip.gov"">
               <soapenv:Header/>
               <soapenv:Body>
                  <wsaa:loginCms>
                     <wsaa:in0>{cms}</wsaa:in0>
                  </wsaa:loginCms>
               </soapenv:Body>
            </soapenv:Envelope>";

            var content = new StringContent(soapRequest, Encoding.UTF8, "text/xml");
            content.Headers.Add("SOAPAction", "http://wsaa.view.sua.dvadac.desein.afip.gov/loginCms");
            string urlWsaa = _configuration["Afip:urlWsaa"];
            var response = await _httpClient.PostAsync(urlWsaa, content);

            var responseContent = await response.Content.ReadAsStringAsync();
            return responseContent;
        }
        public (string Token, string Sign) ParseLoginCmsResponse(string xmlResponse)
        {
            // Crear un objeto XmlDocument
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xmlResponse);

            // Extraer el contenido del campo loginCmsReturn
            XmlNode loginCmsReturnNode = doc.GetElementsByTagName("loginCmsReturn")[0];
            string loginCmsReturnContent = loginCmsReturnNode.InnerText;

            // Decodificar las entidades HTML (el contenido está en formato HTML encodeado)
            string decodedXml = System.Web.HttpUtility.HtmlDecode(loginCmsReturnContent);

            // Cargar el XML decodificado en un nuevo XmlDocument
            XmlDocument loginTicketResponseDoc = new XmlDocument();
            loginTicketResponseDoc.LoadXml(decodedXml);

            if (loginTicketResponseDoc.DocumentElement != null)
            {
                // Extraer el token y el sign
                string token = loginTicketResponseDoc.GetElementsByTagName("token")[0].InnerText;
                string sign = loginTicketResponseDoc.GetElementsByTagName("sign")[0].InnerText;

                if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(sign))
                    throw new Exception("No se encontraron los elementos token o sign en la respuesta SOAP.");

                return (token, sign);
            }
            else { throw new Exception("No se encontraron los elementos token o sign en la respuesta SOAP."); }
        }

        public async Task<FECAESolicitarResponse> FECAESolicitarAsync(int IdSucursal, ComprobanteData comprobanteData)
        {
            try
            {
                // Autenticar y obtener token y sign
                var datosAfip = await AuthenticateAsync(IdSucursal);

                

                // Crear el cuerpo de la solicitud SOAP con los datos del comprobante
                var soapRequest = $@"
                        <soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:ar=""http://ar.gov.afip.dif.FEV1/"">
                           <soapenv:Header/>
                           <soapenv:Body>
                              <ar:FECAESolicitar>
                                 <ar:Auth>
                                    <ar:Token>{datosAfip.Token}</ar:Token>
                                    <ar:Sign>{datosAfip.Sign}</ar:Sign>
                                    <ar:Cuit>{datosAfip.CuitFactura}</ar:Cuit>
                                 </ar:Auth>
                                 <ar:FeCAEReq>
                                    <ar:FeCabReq>
                                       <ar:CantReg>1</ar:CantReg>
                                       <ar:PtoVta>{datosAfip.PuntoDeVenta}</ar:PtoVta>
                                       <ar:CbteTipo>{comprobanteData.CbteTipo}</ar:CbteTipo>
                                    </ar:FeCabReq>
                                    <ar:FeDetReq>
                                       <ar:FECAEDetRequest>
                                          <ar:Concepto>{comprobanteData.Concepto}</ar:Concepto>
                                          <ar:DocTipo>{comprobanteData.DocTipo}</ar:DocTipo>
                                          <ar:DocNro>{comprobanteData.DocNro}</ar:DocNro>
                                          <ar:CbteDesde>{comprobanteData.CbteDesde}</ar:CbteDesde>
                                          <ar:CbteHasta>{comprobanteData.CbteHasta}</ar:CbteHasta>
                                          <ar:CbteFch>{comprobanteData.CbteFch}</ar:CbteFch>
                                          <ar:ImpTotal>{comprobanteData.ImpTotal}</ar:ImpTotal>
                                          <ar:ImpTotConc>{0}</ar:ImpTotConc>
                                          <ar:ImpNeto>{comprobanteData.ImpTotal}</ar:ImpNeto>
                                          <ar:ImpOpEx>{0}</ar:ImpOpEx>
                                          <ar:ImpTrib>{0}</ar:ImpTrib>
                                          <ar:ImpIVA>{0}</ar:ImpIVA>
                                          <ar:MonId>{comprobanteData.MonId}</ar:MonId>
                                          <ar:MonCotiz>{comprobanteData.MonCotiz}</ar:MonCotiz>
                                       </ar:FECAEDetRequest>
                                    </ar:FeDetReq>
                                 </ar:FeCAEReq>
                              </ar:FECAESolicitar>
                           </soapenv:Body>
                        </soapenv:Envelope>";

                // Enviar la solicitud SOAP a AFIP
                var content = new StringContent(soapRequest, Encoding.UTF8, "text/xml");

                HttpResponseMessage response;
                try
                {
                    string urlWsfev1 = _configuration["Afip:urlWsfev1"];
                    response = await _httpClient.PostAsync(urlWsfev1, content);
                    response.EnsureSuccessStatusCode();
                }
                catch (TaskCanceledException e)
                {
                    // Manejar time-out
                    return await ManejarTimeOutAsync(comprobanteData, datosAfip);
                }

                // Leer la respuesta de AFIP
                var responseContent = await response.Content.ReadAsStringAsync();

                // Parsear la respuesta y retornar el resultado
                return ParseComprobanteResponse(responseContent);
            }
            catch (Exception e)
            {
                throw new Exception("Error al generar el comprobante", e);
            }
        }

        private async Task<FECAESolicitarResponse> ManejarTimeOutAsync(ComprobanteData comprobanteData, ParamAfip datosAfip)
        {
            // Intentar consultar el comprobante emitido por punto de venta, tipo y número
            var soapRequestConsulta = $@"
            <soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:ar=""http://ar.gov.afip.dif.FEV1/"">
               <soapenv:Header/>
               <soapenv:Body>
                  <ar:FECompConsultar>
                     <ar:Auth>
                        <ar:Token>{datosAfip.Token}</ar:Token>
                        <ar:Sign>{datosAfip.Sign}</ar:Sign>
                        <ar:Cuit>{datosAfip.CuitFactura}</ar:Cuit>
                     </ar:Auth>
                     <ar:FeCompConsReq>
                        <ar:PtoVta>{datosAfip.PuntoDeVenta}</ar:PtoVta>
                        <ar:CbteTipo>{comprobanteData.CbteTipo}</ar:CbteTipo>
                        <ar:CbteNro>{comprobanteData.CbteDesde}</ar:CbteNro>
                     </ar:FeCompConsReq>
                  </ar:FECompConsultar>
               </soapenv:Body>
            </soapenv:Envelope>";

            string urlWsfev1 = _configuration["Afip:urlWsfev1"];
            var contentConsulta = new StringContent(soapRequestConsulta, Encoding.UTF8, "text/xml");
            var responseConsulta = await _httpClient.PostAsync(urlWsfev1 +"?op=FECompConsultar", contentConsulta);

            if (responseConsulta.IsSuccessStatusCode)
            {
                var responseContentConsulta = await responseConsulta.Content.ReadAsStringAsync();
                FECAESolicitarResponse result = ParseComprobanteResponse(responseContentConsulta);

                if (!string.IsNullOrEmpty(result.Cae))
                {
                    return result;
                }
            }

            // Si no se encontró información, devolver error indicando que la operación no fue completada
            throw new Exception("Error de comunicación: no se pudo confirmar la emisión del comprobante.");
        }

        public FECAESolicitarResponse ParseComprobanteResponse(string xmlResponse)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xmlResponse);

            if (doc.DocumentElement != null)
            {
                // Extraer información de la respuesta
                XmlNode resultadoNode = doc.GetElementsByTagName("Resultado")[0];
                string resultado = resultadoNode.InnerText;

                if (resultado == "A")
                {
                    XmlNode caeNode = doc.GetElementsByTagName("CAE")[0];
                    XmlNode fechaVtoCaeNode = doc.GetElementsByTagName("CAEFchVto")[0];
                    XmlNode ResultadoNode = doc.GetElementsByTagName("Resultado")[0];
                    FECAESolicitarResponse result = new FECAESolicitarResponse();
                    result.Cae = caeNode.InnerText;
                    result.FechaVtoCae = fechaVtoCaeNode.InnerText;
                    result.Resultado = ResultadoNode.InnerText;

                    if (string.IsNullOrEmpty(result.Cae) || string.IsNullOrEmpty(result.FechaVtoCae))
                        throw new Exception("No se encontraron los elementos cae o fechaVtoCae en la respuesta SOAP.");

                    return result;
                }
                else
                {
                    XmlNode errorMsgNode = doc.GetElementsByTagName("Msg")[0];
                    string errorMsg = errorMsgNode.InnerText;

                    throw new Exception($"Error al generar comprobante: {errorMsg}");
                }
            }
            else { throw new Exception("No se encontraron los elementos en resultado de FECAESolicitar en la respuesta SOAP."); }

        }

        public async Task<string> FEDummyAsync()
        {
            // Cuerpo del mensaje SOAP
            var soapRequest = @"<soapenv:Envelope xmlns:soapenv='http://schemas.xmlsoap.org/soap/envelope/' xmlns:ar='http://ar.gov.afip.dif.FEV1/'>
                                <soapenv:Header/>
                                <soapenv:Body>
                                    <ar:FEDummy/>
                                </soapenv:Body>
                            </soapenv:Envelope>";

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    // Crear el contenido de la solicitud
                    var content = new StringContent(soapRequest, Encoding.UTF8, "text/xml");

                    // Agregar headers si es necesario
                    content.Headers.Add("SOAPAction", "http://ar.gov.afip.dif.FEV1/FEDummy");

                    string urlWsfev1 = _configuration["Afip:urlWsfev1"];
                    
                    // Hacer la solicitud POST
                    HttpResponseMessage response = await client.PostAsync(urlWsfev1, content);

                    // Verificar si la respuesta fue exitosa
                    response.EnsureSuccessStatusCode();

                    // Obtener el contenido de la respuesta como string
                    string responseString = await response.Content.ReadAsStringAsync();

                    // Crear un objeto XmlDocument
                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml(responseString);
                    var ambiente = doc.GetElementsByTagName("ambiente")[0].InnerText;
                    var appServer = doc.GetElementsByTagName("AppServer")[0].InnerText;
                    var dbServer = doc.GetElementsByTagName("DbServer")[0].InnerText;
                    var authServer = doc.GetElementsByTagName("AuthServer")[0].InnerText;

                    // Devolver la respuesta en el formato que desees
                    return $"Ambiente: {ambiente}, AppServer: {appServer}, DbServer: {dbServer}, AuthServer: {authServer}";
                }
            }
            catch (Exception ex)
            {
                // Manejar excepciones en caso de error
                return $"Error: {ex.Message}";
            }
        }

        public async Task<FETipoParametroResponse> FEParametrosAfipAsync(string nombreFuncion,int idSucursal)
        {
            try
            {
                var datosAfip = await AuthenticateAsync(idSucursal);

                // Crear el cuerpo de la solicitud SOAP
                var soapRequest = $@"
                <soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:ar=""http://ar.gov.afip.dif.FEV1/"">
                   <soapenv:Header/>
                   <soapenv:Body>
                      <ar:{nombreFuncion}>
                         <ar:Auth>
                            <ar:Token>{datosAfip.Token}</ar:Token>
                            <ar:Sign>{datosAfip.Sign}</ar:Sign>
                            <ar:Cuit>{datosAfip.CuitFactura}</ar:Cuit>
                         </ar:Auth>
                      </ar:{nombreFuncion}>
                   </soapenv:Body>
                </soapenv:Envelope>";

                // Configurar la solicitud HTTP
                string urlWsfev1 = _configuration["Afip:urlWsfev1"];
                var content = new StringContent(soapRequest, Encoding.UTF8, "text/xml");
                var requestUri = urlWsfev1 + "?op="+ nombreFuncion;

                // Enviar la solicitud POST
                HttpResponseMessage response = await _httpClient.PostAsync(requestUri, content);
                response.EnsureSuccessStatusCode();

                // Leer el contenido de la respuesta
                var responseContent = await response.Content.ReadAsStringAsync();

                // Procesar y retornar la respuesta (aquí podrías parsear el XML si lo necesitas)
                return ParseTipoParametroResponse(nombreFuncion,responseContent);
            }
            catch (Exception ex)
            {
                throw new Exception("Error al obtener los tipos de comprobantes", ex);
            }
        }
        public FETipoParametroResponse ParseTipoParametroResponse(string nombreFuncion, string responseXml)
        {
            XDocument doc = XDocument.Parse(responseXml);
            var result = new FETipoParametroResponse();

            string nombreTipo = "";

            switch (nombreFuncion)
            {
                case "FEParamGetTiposCbte":
                        nombreTipo = "CbteTipo";
                    break;      
                case "FEParamGetTiposConcepto":
                        nombreTipo = "ConceptoTipo";
                    break;
                case "FEParamGetTiposDoc":
                        nombreTipo = "DocTipo";
                    break; 
                case "FEParamGetTiposIva":
                        nombreTipo = "IvaTipo";
                    break;  
                case "FEParamGetTiposTributos":
                        nombreTipo = "TributoTipo";
                    break;
                default:
                    throw new Exception("Tipo de parametro no tipificado");
                    
            }

            var nodos = doc.Descendants("{http://ar.gov.afip.dif.FEV1/}"+nombreTipo);
            foreach (var nodo in nodos)
            {
                var tipo = new TipoParametro
                {
                    Id = (int)nodo.Element("{http://ar.gov.afip.dif.FEV1/}Id"),
                    Desc = (string)nodo.Element("{http://ar.gov.afip.dif.FEV1/}Desc"),
                    FchDesde = (string)nodo.Element("{http://ar.gov.afip.dif.FEV1/}FchDesde"),
                    FchHasta = (string)nodo.Element("{http://ar.gov.afip.dif.FEV1/}FchHasta")
                };
                result.TiposParametro.Add(tipo);
            }

            // Parseo de los errores
            var errores = doc.Descendants("{http://ar.gov.afip.dif.FEV1/}Err");
            foreach (var error in errores)
            {
                var errorAfip = new ErrorAFIP
                {
                    Code = (int)error.Element("{http://ar.gov.afip.dif.FEV1/}Code"),
                    Msg = (string)error.Element("{http://ar.gov.afip.dif.FEV1/}Msg")
                };
                result.Errors.Add(errorAfip);
            }

            // Parseo de los eventos
            var eventos = doc.Descendants("{http://ar.gov.afip.dif.FEV1/}Evt");
            foreach (var evento in eventos)
            {
                var eventoAfip = new EventoAFIP
                {
                    Code = (int)evento.Element("{http://ar.gov.afip.dif.FEV1/}Code"),
                    Msg = (string)evento.Element("{http://ar.gov.afip.dif.FEV1/}Msg")
                };
                result.Events.Add(eventoAfip);
            }


            return result;
        }
        public async Task<FETipoParametro2Response> FEParamGetTiposMonedasAsync(int idSucursal)
        {
            try
            {
                var datosAfip = await AuthenticateAsync(idSucursal);

                // Crear el cuerpo de la solicitud SOAP
                var soapRequest = $@"
                <soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:ar=""http://ar.gov.afip.dif.FEV1/"">
                   <soapenv:Header/>
                   <soapenv:Body>
                      <ar:FEParamGetTiposMonedas>
                         <ar:Auth>
                            <ar:Token>{datosAfip.Token}</ar:Token>
                            <ar:Sign>{datosAfip.Sign}</ar:Sign>
                            <ar:Cuit>{datosAfip.CuitFactura}</ar:Cuit>
                         </ar:Auth>
                      </ar:FEParamGetTiposMonedas>
                   </soapenv:Body>
                </soapenv:Envelope>";

                // Configurar la solicitud HTTP
                string urlWsfev1 = _configuration["Afip:urlWsfev1"];
                var content = new StringContent(soapRequest, Encoding.UTF8, "text/xml");
                var requestUri = urlWsfev1 + "?op=FEParamGetTiposMonedas";

                // Enviar la solicitud POST
                HttpResponseMessage response = await _httpClient.PostAsync(requestUri, content);
                response.EnsureSuccessStatusCode();

                // Leer el contenido de la respuesta
                var responseContent = await response.Content.ReadAsStringAsync();

                // Procesar y retornar la respuesta (aquí podrías parsear el XML si lo necesitas)
                return ParseTipoMonedaResponse(responseContent);
            }
            catch (Exception ex)
            {
                throw new Exception("Error al obtener los tipos de comprobantes", ex);
            }
        }
        public FETipoParametro2Response ParseTipoMonedaResponse(string responseXml)
        {
            XDocument doc = XDocument.Parse(responseXml);
            var result = new FETipoParametro2Response();

            var nodos = doc.Descendants("{http://ar.gov.afip.dif.FEV1/}Moneda");
            foreach (var nodo in nodos)
            {
                var tipo = new TipoParametro2
                {
                    Id = (string)nodo.Element("{http://ar.gov.afip.dif.FEV1/}Id"),
                    Desc = (string)nodo.Element("{http://ar.gov.afip.dif.FEV1/}Desc"),
                    FchDesde = (string)nodo.Element("{http://ar.gov.afip.dif.FEV1/}FchDesde"),
                    FchHasta = (string)nodo.Element("{http://ar.gov.afip.dif.FEV1/}FchHasta")
                };
                result.TiposParametro.Add(tipo);
            }


            // Parseo de los errores
            var errores = doc.Descendants("{http://ar.gov.afip.dif.FEV1/}Err");
            foreach (var error in errores)
            {
                var errorAfip = new ErrorAFIP
                {
                    Code = (int)error.Element("{http://ar.gov.afip.dif.FEV1/}Code"),
                    Msg = (string)error.Element("{http://ar.gov.afip.dif.FEV1/}Msg")
                };
                result.Errors.Add(errorAfip);
            }

            // Parseo de los eventos
            var eventos = doc.Descendants("{http://ar.gov.afip.dif.FEV1/}Evt");
            foreach (var evento in eventos)
            {
                var eventoAfip = new EventoAFIP
                {
                    Code = (int)evento.Element("{http://ar.gov.afip.dif.FEV1/}Code"),
                    Msg = (string)evento.Element("{http://ar.gov.afip.dif.FEV1/}Msg")
                };
                result.Events.Add(eventoAfip);
            }

            return result;
        }  
        public async Task<FETipoParametro2Response> FEParamGetTiposOpcionalAsync(int idSucursal)
        {
            try
            {
                var datosAfip = await AuthenticateAsync(idSucursal);

                // Crear el cuerpo de la solicitud SOAP
                var soapRequest = $@"
                <soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:ar=""http://ar.gov.afip.dif.FEV1/"">
                   <soapenv:Header/>
                   <soapenv:Body>
                      <ar:FEParamGetTiposOpcional>
                         <ar:Auth>
                            <ar:Token>{datosAfip.Token}</ar:Token>
                            <ar:Sign>{datosAfip.Sign}</ar:Sign>
                            <ar:Cuit>{datosAfip.CuitFactura}</ar:Cuit>
                         </ar:Auth>
                      </ar:FEParamGetTiposOpcional>
                   </soapenv:Body>
                </soapenv:Envelope>";

                // Configurar la solicitud HTTP
                string urlWsfev1 = _configuration["Afip:urlWsfev1"];
                var content = new StringContent(soapRequest, Encoding.UTF8, "text/xml");
                var requestUri = urlWsfev1 + "?op=FEParamGetTiposOpcional";

                // Enviar la solicitud POST
                HttpResponseMessage response = await _httpClient.PostAsync(requestUri, content);
                response.EnsureSuccessStatusCode();

                // Leer el contenido de la respuesta
                var responseContent = await response.Content.ReadAsStringAsync();

                // Procesar y retornar la respuesta (aquí podrías parsear el XML si lo necesitas)
                return ParseTiposOpcionalesResponse(responseContent);
            }
            catch (Exception ex)
            {
                throw new Exception("Error al obtener los tipos de comprobantes", ex);
            }
        }
        public FETipoParametro2Response ParseTiposOpcionalesResponse(string responseXml)
        {
            XDocument doc = XDocument.Parse(responseXml);
            var result = new FETipoParametro2Response();

            var nodos = doc.Descendants("{http://ar.gov.afip.dif.FEV1/}OpcionalTipo");
            foreach (var nodo in nodos)
            {
                var tipo = new TipoParametro2
                {
                    Id = (string)nodo.Element("{http://ar.gov.afip.dif.FEV1/}Id"),
                    Desc = (string)nodo.Element("{http://ar.gov.afip.dif.FEV1/}Desc"),
                    FchDesde = (string)nodo.Element("{http://ar.gov.afip.dif.FEV1/}FchDesde"),
                    FchHasta = (string)nodo.Element("{http://ar.gov.afip.dif.FEV1/}FchHasta")
                };
                result.TiposParametro.Add(tipo);
            }

            // Parseo de los errores
            var errores = doc.Descendants("{http://ar.gov.afip.dif.FEV1/}Err");
            foreach (var error in errores)
            {
                var errorAfip = new ErrorAFIP
                {
                    Code = (int)error.Element("{http://ar.gov.afip.dif.FEV1/}Code"),
                    Msg = (string)error.Element("{http://ar.gov.afip.dif.FEV1/}Msg")
                };
                result.Errors.Add(errorAfip);
            }

            // Parseo de los eventos
            var eventos = doc.Descendants("{http://ar.gov.afip.dif.FEV1/}Evt");
            foreach (var evento in eventos)
            {
                var eventoAfip = new EventoAFIP
                {
                    Code = (int)evento.Element("{http://ar.gov.afip.dif.FEV1/}Code"),
                    Msg = (string)evento.Element("{http://ar.gov.afip.dif.FEV1/}Msg")
                };
                result.Events.Add(eventoAfip);
            }

            return result;
        }
        public async Task<FEParamGetPtosVentaResponse> FEParamGetPtosVentaAsync(int idSucursal)
        {
            try
            {
                var datosAfip = await AuthenticateAsync(idSucursal);

                // Crear el cuerpo de la solicitud SOAP
                var soapRequest = $@"
                <soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:ar=""http://ar.gov.afip.dif.FEV1/"">
                   <soapenv:Header/>
                   <soapenv:Body>
                      <ar:FEParamGetPtosVenta>
                         <ar:Auth>
                            <ar:Token>{datosAfip.Token}</ar:Token>
                            <ar:Sign>{datosAfip.Sign}</ar:Sign>
                            <ar:Cuit>{datosAfip.CuitFactura}</ar:Cuit>
                         </ar:Auth>
                      </ar:FEParamGetPtosVenta>
                   </soapenv:Body>
                </soapenv:Envelope>";

                // Configurar la solicitud HTTP
                string urlWsfev1 = _configuration["Afip:urlWsfev1"];
                var content = new StringContent(soapRequest, Encoding.UTF8, "text/xml");
                var requestUri = urlWsfev1 + "?op=FEParamGetPtosVenta";

                // Enviar la solicitud POST
                HttpResponseMessage response = await _httpClient.PostAsync(requestUri, content);
                response.EnsureSuccessStatusCode();

                // Leer el contenido de la respuesta
                var responseContent = await response.Content.ReadAsStringAsync();

                // Procesar y retornar la respuesta (aquí podrías parsear el XML si lo necesitas)
                return ParsePuntosDeVentaResponse(responseContent);
            }
            catch (Exception ex)
            {
                throw new Exception("Error al obtener los tipos de comprobantes", ex);
            }
        }
        public FEParamGetPtosVentaResponse ParsePuntosDeVentaResponse(string responseXml)
        {
            XDocument doc = XDocument.Parse(responseXml);
            var result = new FEParamGetPtosVentaResponse();

            var nodos = doc.Descendants("{http://ar.gov.afip.dif.FEV1/}PtoVenta");
            foreach (var nodo in nodos)
            {
                var tipo = new TipoPuntoDeVenta
                {
                    Nro = (int)nodo.Element("{http://ar.gov.afip.dif.FEV1/}Nro"),
                    EmisionTipo = (string)nodo.Element("{http://ar.gov.afip.dif.FEV1/}EmisionTipo"),
                    Bloqueado = (string)nodo.Element("{http://ar.gov.afip.dif.FEV1/}Bloqueado"),
                    FchBaja = (string)nodo.Element("{http://ar.gov.afip.dif.FEV1/}FchBaja")
                };
                result.TipospuntoDeVenta.Add(tipo);
            }

            // Parseo de los errores
            var errores = doc.Descendants("{http://ar.gov.afip.dif.FEV1/}Err");
            foreach (var error in errores)
            {
                var errorAfip = new ErrorAFIP
                {
                    Code = (int)error.Element("{http://ar.gov.afip.dif.FEV1/}Code"),
                    Msg = (string)error.Element("{http://ar.gov.afip.dif.FEV1/}Msg")
                };
                result.Errors.Add(errorAfip);
            }

            // Parseo de los eventos
            var eventos = doc.Descendants("{http://ar.gov.afip.dif.FEV1/}Evt");
            foreach (var evento in eventos)
            {
                var eventoAfip = new EventoAFIP
                {
                    Code = (int)evento.Element("{http://ar.gov.afip.dif.FEV1/}Code"),
                    Msg = (string)evento.Element("{http://ar.gov.afip.dif.FEV1/}Msg")
                };
                result.Events.Add(eventoAfip);
            }

            return result;
        }
        public async Task<FECompUltimoAutorizadoResponse> FECompUltimoAutorizadoAsync(int idSucursal, int tipoComprobante = 11)
        {
            try
            {
                var datosAfip = await AuthenticateAsync(idSucursal);

                // Crear el cuerpo de la solicitud SOAP
                var soapRequest = $@"
                <soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:ar=""http://ar.gov.afip.dif.FEV1/"">
                   <soapenv:Header/>
                   <soapenv:Body>
                      <ar:FECompUltimoAutorizado>
                         <ar:Auth>
                            <ar:Token>{datosAfip.Token}</ar:Token>
                            <ar:Sign>{datosAfip.Sign}</ar:Sign>
                            <ar:Cuit>{datosAfip.CuitFactura}</ar:Cuit>
                         </ar:Auth>
                         <ar:PtoVta>{datosAfip.PuntoDeVenta}</ar:PtoVta>
                         <ar:CbteTipo>{tipoComprobante}</ar:CbteTipo>
                      </ar:FECompUltimoAutorizado>
                   </soapenv:Body>
                </soapenv:Envelope>";

                // Configurar la solicitud HTTP
                string urlWsfev1 = _configuration["Afip:urlWsfev1"];
                var content = new StringContent(soapRequest, Encoding.UTF8, "text/xml");
                var requestUri = urlWsfev1 + "?op=FECompUltimoAutorizado";

                // Enviar la solicitud POST
                HttpResponseMessage response = await _httpClient.PostAsync(requestUri, content);
                response.EnsureSuccessStatusCode();

                // Leer el contenido de la respuesta
                var responseContent = await response.Content.ReadAsStringAsync();

                // Procesar y retornar la respuesta (aquí podrías parsear el XML si lo necesitas)
                return ParseFECompUltimoAutorizadoResponse(responseContent);
            }
            catch (Exception ex)
            {
                throw new Exception("Error al obtener los tipos de comprobantes", ex);
            }
        }
        public FECompUltimoAutorizadoResponse ParseFECompUltimoAutorizadoResponse(string responseXml)
        {
            XDocument doc = XDocument.Parse(responseXml);
            var result = new FECompUltimoAutorizadoResponse();

            var nodos = doc.Descendants("{http://ar.gov.afip.dif.FEV1/}FECompUltimoAutorizadoResult");
            foreach (var nodo in nodos)
            {
                var tipo = new FEUltimoComprobanteAutorizado
                {
                    PtoVta = (int)nodo.Element("{http://ar.gov.afip.dif.FEV1/}PtoVta"),
                    CbteTipo = (int)nodo.Element("{http://ar.gov.afip.dif.FEV1/}CbteTipo"),
                    CbteNro = (Int64)nodo.Element("{http://ar.gov.afip.dif.FEV1/}CbteNro")
                };
                result.comprobante.Add(tipo);
            }

            // Parseo de los errores
            var errores = doc.Descendants("{http://ar.gov.afip.dif.FEV1/}Err");
            foreach (var error in errores)
            {
                var errorAfip = new ErrorAFIP
                {
                    Code = (int)error.Element("{http://ar.gov.afip.dif.FEV1/}Code"),
                    Msg = (string)error.Element("{http://ar.gov.afip.dif.FEV1/}Msg")
                };
                result.Errors.Add(errorAfip);
            }

            // Parseo de los eventos
            var eventos = doc.Descendants("{http://ar.gov.afip.dif.FEV1/}Evt");
            foreach (var evento in eventos)
            {
                var eventoAfip = new EventoAFIP
                {
                    Code = (int)evento.Element("{http://ar.gov.afip.dif.FEV1/}Code"),
                    Msg = (string)evento.Element("{http://ar.gov.afip.dif.FEV1/}Msg")
                };
                result.Events.Add(eventoAfip);
            }

            return result;
        }

        public async Task<string> FECompConsultarAsync(int idSucursal, int tipoComprobante=11,Int64 nroComprobante = 1)
        {
            try
            {
                var datosAfip = await AuthenticateAsync(idSucursal);

                // Crear el cuerpo de la solicitud SOAP
                var soapRequest = $@"
                <soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:ar=""http://ar.gov.afip.dif.FEV1/"">
                   <soapenv:Header/>
                   <soapenv:Body>
                      <ar:FECompConsultar>
                         <ar:Auth>
                            <ar:Token>{datosAfip.Token}</ar:Token>
                            <ar:Sign>{datosAfip.Sign}</ar:Sign>
                            <ar:Cuit>{datosAfip.CuitFactura}</ar:Cuit>
                         </ar:Auth>
                         <ar:FeCompConsReq>
                             <ar:CbteTipo>{tipoComprobante}</ar:CbteTipo>
                             <ar:CbteNro>{nroComprobante}</ar:CbteNro>
                             <ar:PtoVta>{datosAfip.PuntoDeVenta}</ar:PtoVta>
                         </ar:FeCompConsReq>
                      </ar:FECompConsultar>
                   </soapenv:Body>
                </soapenv:Envelope>";

                // Configurar la solicitud HTTP
                string urlWsfev1 = _configuration["Afip:urlWsfev1"];
                var content = new StringContent(soapRequest, Encoding.UTF8, "text/xml");
                var requestUri = urlWsfev1 + "?op=FECompConsultar";

                // Enviar la solicitud POST
                HttpResponseMessage response = await _httpClient.PostAsync(requestUri, content);
                response.EnsureSuccessStatusCode();

                // Leer el contenido de la respuesta
                var responseContent = await response.Content.ReadAsStringAsync();

                // Procesar y retornar la respuesta (aquí podrías parsear el XML si lo necesitas)
                return responseContent;
            }
            catch (Exception ex)
            {
                throw new Exception("Error al obtener los tipos de comprobantes", ex);
            }
        }

        public async Task<object> GenerarComprobanteYEnviarAlCliente(Int64 idVenta, int idUsuario)
        {
            try
            {
                // 1. Obtener la información de la venta
                var venta = await _context.Venta.FirstAsync(v => v.IdVenta == idVenta);
                string contenidoHtml = "";
                if (venta != null)
                {
                    var detalles = _context.VentaDetalles
                        .Include(p => p.IdProductoNavigation)
                        .Include(p => p.IdProductoNavigation.IdTipoProductoNavigation)
                        .Include(p => p.IdProductoNavigation.IdTipoMarcaNavigation)
                        .Include(p => p.IdProductoNavigation.IdTipoProductoNavigation)
                        .Where(d => d.IdVenta == idVenta).ToList();

                    var formasDePago = _context.VentaFormasDePagos
                        .Include(vfp => vfp.IdTipoFormaPagoNavigation).Where(f => f.IdVenta == idVenta).ToList();

                    // 2. Informacion del cliente, ir contra Afip.
                    // 2.1 Actualizar data cliente afip.
                    var cliente = _context.Clientes.Find(venta.IdCliente);
                    //var usuario = _context.Usuarios.Find(venta.IdUsuario);
                    string denominacionCliente = "Consumidor Final";
                    string NroDocumento = "0";
                    int tipoDocumento = 99; //para cuando no tenemos el nroDocumento
                    if (cliente != null)
                    {
                        denominacionCliente = $"{cliente.Apellido}, {cliente.Nombre}";
                        NroDocumento = cliente.NroDocumento;
                        tipoDocumento = 96; //DNI
                    }
                    
                    // 2. Calcular el total de productos y formas de pago
                    var totalProductos = detalles.Sum(d => d.Cantidad * d.Valor); //Subtotal
                    var totalFormasDePago = formasDePago.Sum(f => f.ValorParcial);

                    // Calcular la diferencia como descuento
                    var descuento = totalProductos - totalFormasDePago;

                    // Informacion del Sucursal.
                    var datosAfip = await _context.ParamAfip.FirstAsync(v => v.IdSucursal == venta.IdSucursal);
                    long nroComprobante = datosAfip.UltimoComprobanteAprobado + 1;

                    ComprobanteData datosFactura = new ComprobanteData();
                        datosFactura.CbteTipo = 11; //Factura C
                        datosFactura.Concepto = 1; //Producto
                        datosFactura.DenominacionCliente = denominacionCliente;
                        datosFactura.DocTipo = tipoDocumento; 
                        datosFactura.DocNro = Convert.ToInt64(NroDocumento);
                        datosFactura.CbteDesde = nroComprobante;
                        datosFactura.CbteHasta = nroComprobante;
                        datosFactura.CbteFch = DateTime.Now.ToString("yyyyMMdd");
                        datosFactura.ImpTotal = Convert.ToDouble(totalFormasDePago);

                    FECAESolicitarResponse resul = await FECAESolicitarAsync(venta.IdSucursal, datosFactura);

                    if (resul != null && resul.Resultado == "A")
                    {

                        datosAfip.UltimoComprobanteAprobado = nroComprobante;
                        _context.SaveChanges();

                        contenidoHtml = GenerarFactura(datosFactura, datosAfip, resul, detalles, descuento, totalProductos);

                        // Agregar la Comprobante en la tabla Comprobante
                        Comprobante nuevoComprobante = new Comprobante
                        {
                            IdSucursal = venta.IdSucursal,
                            Fecha = DateTime.Now,
                            NroPuntoVenta = datosAfip.PuntoDeVenta,
                            NroComprobante = nroComprobante,
                            Tipo = 'C',
                            Total = totalFormasDePago,
                            FacturaHtml = contenidoHtml,
                            CreatedDate = DateTime.Now,
                            IdUsuario = idUsuario
                        };

                        _context.Comprobante.Add(nuevoComprobante);
                        await _context.SaveChangesAsync();
                        venta.IdComprobante = nuevoComprobante.IdComprobante;
                        await _context.SaveChangesAsync();
                        //Envio de mail
                        string subject = "Maria Moda Circular - Compra N° " + venta.IdVenta.ToString();
                        string body = "Cuerpo del correo - test";

                        SendMailLogic sendMailLogic = new SendMailLogic(_configuration);
                        //await sendMailLogic.SendEmailFacturaWithOAuth2Async(cliente.Mail, subject, body, contenidoHtml);
                    }
                    else {
                        return new { result = "error", message = "Ocurrio un error al procesar el envio del comprobante. Resultado de solicitud CAE: \""+resul.Resultado+"\"" };
                    }


                }
                else
                {
                    return new { result = "error", message = "Ocurrio un error al procesar el envio del comprobante. Venta no encontrada (" + idVenta.ToString() + ")" };
                }
                return new { result = "ok", message = "El archivo se proceso correctamente", comprobante = contenidoHtml };

            }
            catch (Exception)
            {
                return new { result = "error", message = "Ocurrio un error al procesar el envio del comprobante" };
            }

        }

        public async Task<byte[]> generarFactura(int idComprobante)
        {
            // 1. Buscar el comprobante en la base de datos usando Entity Framework
            var comprobante = await _context.Comprobante
                .FirstOrDefaultAsync(v => v.IdComprobante == idComprobante);

            if (comprobante == null)
            {
                throw new Exception("Comprobante no encontrado." );
            }

            // 2. Convertir el HTML del comprobante a PDF usando SelectPdf
            string htmlContent = comprobante.FacturaHtml; // Asumiendo que tienes el HTML en este campo

            // Inicializar el convertidor de HTML a PDF            

            var converter = new SynchronizedConverter(new PdfTools());
            var doc = new HtmlToPdfDocument
            {
                GlobalSettings = {
                ColorMode = ColorMode.Color,
                Orientation = Orientation.Portrait,
                PaperSize = PaperKind.A4
            },
                Objects = {
                new ObjectSettings {
                    PagesCount = true,
                    HtmlContent = htmlContent,
                    WebSettings = { DefaultEncoding = "utf-8" }
                }
            }
            };

            byte[] pdfBytes = converter.Convert(doc);

            // 4. Devolver el archivo PDF para que sea descargado
            return pdfBytes;
        }

        public string GenerarFactura(ComprobanteData datosFactura, ParamAfip datosAfip, FECAESolicitarResponse Feresult, List<VentaDetalle> detalle,decimal descuento, decimal subtotal)
        {
            // Obtener el directorio actual del proyecto
            string rutaProyecto = Directory.GetCurrentDirectory();

            // Combinar para obtener la ruta relativa del archivo HTML
            string rutaPlantilla = Path.Combine(rutaProyecto, "AFIP\\PlantillaFactura\\", "PlantillaFactura.html");

            // Cargar el contenido del archivo HTML
            string htmlFactura = File.ReadAllText(rutaPlantilla);

            // Reemplazar los placeholders con valores dinámicos
            htmlFactura = htmlFactura.Replace("{TipoFactura}", datosAfip.TipoFactura)
                                     .Replace("{NombreFantasia}", datosAfip.NombreFantasia)
                                     .Replace("{RazonSocial}", datosAfip.RazonSocial)
                                     .Replace("{Domicilio}", datosAfip.Domicilio)
                                     .Replace("{CondicionFrenteAlIva}", datosAfip.CondicionFrenteAlIva)
                                     .Replace("{NroPuntoDeVenta}", datosAfip.PuntoDeVenta.ToString().PadLeft(4,'0'))
                                     .Replace("{NroComprobante}", datosFactura.CbteDesde.ToString().PadLeft(8, '0'))
                                     .Replace("{FechaEmision}", DateTime.Now.ToString("dd/MM/yyyy"))
                                     .Replace("{CUIT}", datosAfip.CuitFactura.Insert(2, "-").Insert(11, "-"))
                                     .Replace("{IIBB}", datosAfip.CuitFactura.Insert(2, "-").Insert(11, "-"))
                                     .Replace("{FechaInicioActividades}", datosAfip.FechaInicioActividades.ToString("dd/MM/yyyy"))
                                     .Replace("{CuitCliente}",datosFactura.DocNro.ToString())
                                     .Replace("{NomYApeCliente}", datosFactura.DenominacionCliente)
                                     .Replace("{CondicionFrenteAlIvaCliente}", "Consumidor Final")
                                     .Replace("{DomicilioCliente}", " - ")
                                     .Replace("{tablaProductos}", generarTablaProductosDetalle(detalle))
                                     .Replace("{SubTotal}", subtotal.ToString("F2"))
                                     .Replace("{Descuento}", descuento.ToString("F2"))
                                     .Replace("{Total}", datosFactura.ImpTotal.ToString("F2"))
                                     .Replace("{CodigoQr}", generarQr(datosFactura, datosAfip, Feresult))
                                     .Replace("{CAE}", Feresult.Cae)
                                     .Replace("{FechaVtoCAE}", Feresult.FechaVtoCae);

            return htmlFactura;
        }

        public string generarTablaProductosDetalle(List<VentaDetalle> detalle)
        {
            string detalleProductos = string.Join("", detalle.Select(d =>
                    $@"
                            <tr>
                                <td>{d.IdProducto.ToString().PadLeft(8,'0')}</td>
                                <td>{d.IdProductoNavigation.IdTipoProductoNavigation.Descripcion} - {d.IdProductoNavigation.IdTipoMarcaNavigation.Descripcion}</td>
                                <td>{d.Cantidad:F2}</td>
                                <td>Unidad</td>
                                <td style=""text-align:right;"">{d.Valor:C2}</td>
                                <td style=""text-align:right;"">{(d.Cantidad * d.Valor):C2}</td>
                            </tr>
                            "));

            string tablaHTML = $@"
                        <table id='TablaProductos'>
                            <tr style=""text-align:left;"">
                                <td>Código</td>
                                <td>Producto</td>
                                <td>Cantidad</td>
                                <td>U. Medida</td>
                                <td>Precio Unit.</td>
                                <td>Subtotal</td>
                            </tr>
                            {detalleProductos}
                        </table>";
            return tablaHTML;
        }

        public string generarQr(ComprobanteData datosFactura,ParamAfip datosAfip, FECAESolicitarResponse Feresult)
        {

            // Datos del comprobante en un objeto JSON
            var comprobante = new
            {
                ver = 1, //version va siempre esa,
                fecha = datosFactura.CbteFch.Insert(4, "-").Insert(7, "-"),  // Cambia a la fecha de emisión real "yyyy-MM-dd"
                cuit = Convert.ToInt64(datosAfip.CuitFactura), //cuit sin guiones
                ptoVta = datosAfip.PuntoDeVenta,
                tipoCmp = datosFactura.CbteTipo,
                nroCmp = datosFactura.CbteDesde,
                importe = datosFactura.ImpTotal,
                moneda = datosFactura.MonId,
                ctz = datosFactura.MonCotiz,
                tipoDocRec = datosFactura.DocTipo,
                nroDocRec = datosFactura.DocNro,
                tipoCodAut = "E", //“A” para comprobante autorizado por CAEA, “E” para comprobante autorizado por CAE,
                codAut = Convert.ToInt64(Feresult.Cae)
            };

            // Serializar el objeto JSON y convertirlo en Base64
            string jsonComprobante = JsonSerializer.Serialize(comprobante);
            string datosCmpBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(jsonComprobante));

            // URL con los datos codificados
            string qrText = $"https://www.afip.gob.ar/fe/qr/?p={datosCmpBase64}";

            // Generar el QR usando QRCoder
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(qrText, QRCodeGenerator.ECCLevel.L);

            PngByteQRCode qrCode = new PngByteQRCode(qrCodeData);  // Usar PngByteQRCode para generar la imagen
            byte[] qrCodeAsPngByteArray = qrCode.GetGraphic(10);  // Generar la imagen en formato PNG

            // Convertir la imagen en base64 para incrustarla en HTML
            string qrBase64 = Convert.ToBase64String(qrCodeAsPngByteArray);

            return $"<img id='qrcode' src='data:image/png;base64,{qrBase64}' alt='QR Comprobante' />";
        }

        public async Task<DummyResponse> PersonaDummyAsync()
        {
            var soapRequest = $@"
            <soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:a13=""http://a13.soap.ws.server.puc.sr/"">
                <soapenv:Header/>
                <soapenv:Body>
                    <a13:dummy/>
                </soapenv:Body>
            </soapenv:Envelope>";
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    // Crear el contenido de la solicitud
                    var content = new StringContent(soapRequest, Encoding.UTF8, "text/xml");

                    string urlWsPadron = _configuration["Afip:urlWsPadron"];

                    // Hacer la solicitud POST
                    HttpResponseMessage response = await client.PostAsync(urlWsPadron, content);

                    // Verificar si la respuesta fue exitosa
                    response.EnsureSuccessStatusCode();

                    // Obtener el contenido de la respuesta como string
                    string responseString = await response.Content.ReadAsStringAsync();

                    return ParsePadronDummyResponse(responseString);
                }
            }
            catch (Exception ex)
            {
                // Manejar excepciones en caso de error
                DummyResponse result = new DummyResponse();
                result.Error = $"Error: {ex.Message}";

                return result;
            }
        }
        private DummyResponse ParsePadronDummyResponse(string responseXml)
        {
            var result = new DummyResponse();
            // Crear un objeto XmlDocument
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(responseXml);

            result.AppServer = xmlDoc.GetElementsByTagName("appserver")[0].InnerText;
            result.AuthServer = xmlDoc.GetElementsByTagName("dbserver")[0].InnerText;
            result.DbServer = xmlDoc.GetElementsByTagName("authserver")[0].InnerText;

            return result;
        }

        public async Task<List<string>> getIdPersonaListByDocumento(int idSucursal,long nroDocumento)
        {
            try
            {
                // Crear una instancia del cliente generado
                var datosAfip = await AuthenticatePadronAsync(idSucursal);
                
                //Crear el cuerpo de la solicitud SOAP
                var soapRequest =
            $@"
                <soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:a13=""http://a13.soap.ws.server.puc.sr/"">
                   <soapenv:Header/>
                   <soapenv:Body>
                      <a13:getIdPersonaListByDocumento>
                        <token>{datosAfip.TokenPadron}</token>
                        <sign>{datosAfip.SignPadron}</sign>
                        <cuitRepresentada>{datosAfip.CuitFactura}</cuitRepresentada>
                        <documento>{nroDocumento}</documento>
                      </a13:getIdPersonaListByDocumento>
                   </soapenv:Body>
                </soapenv:Envelope>";

                // Configurar la solicitud HTTP
                string urlWsPadron = _configuration["Afip:urlWsPadron"];
                // Configurar la solicitud HTTP
                var content = new StringContent(soapRequest, Encoding.UTF8, "text/xml");
                content.Headers.Add("SOAPAction", "");
                var requestUri = urlWsPadron;
                // Enviar la solicitud POST
                HttpResponseMessage response = await _httpClient.PostAsync(requestUri, content);
                //response.EnsureSuccessStatusCode();

                // Leer el contenido de la respuesta
                var responseContent = await response.Content.ReadAsStringAsync();

                // Procesar y retornar la respuesta (aquí podrías parsear el XML si lo necesitas)
                // Manejar la respuesta
                return ParseIdPersonaListResponse(responseContent);
                
            }
            catch (Exception ex)
            {
                throw new Exception("Error al obtener los tipos de comprobantes", ex);
            }
        }

        public List<string> ParseIdPersonaListResponse(string responseXml)
        {
            XDocument doc = XDocument.Parse(responseXml);
            var idPersonas = new List<string>();

            // Verificar si la respuesta contiene un Fault
            XName nameFault = "Fault";
            var fault = doc.Descendants(nameFault).FirstOrDefault();
            if (fault != null)
            {
                XName namefaultcode = "faultcode";
                XName namefaultstring = "faultstring";
                var faultCode = (string)fault.Element(namefaultcode);
                var faultString = (string)fault.Element(namefaultstring);

                Console.WriteLine($"Error SOAP - Código: {faultCode}, Mensaje: {faultString}");
                return idPersonas; // Retorna la lista vacía si hay error
            }

            // Extraer los valores de <idPersona> en caso de respuesta exitosa
            XName namePersona = "idPersona";
            var nodos = doc.Descendants(namePersona);
            foreach (var nodo in nodos)
            {
                idPersonas.Add(nodo.Value);
            }

            // Parsear los metadatos (opcional)
            XName nameMetadata = "metadata";
            var metadata = doc.Descendants(nameMetadata).FirstOrDefault();
            if (metadata != null)
            {
                XName namefechaHora = "fechaHora";
                XName nameservidor = "servidor";
                var fechaHora = (string)metadata.Element(namefechaHora);
                var servidor = (string)metadata.Element(nameservidor);
            }

            return idPersonas;
        }

        // Método para consumir el servicio SOAP getPersona
        public async Task<PersonaResponse> getIdPersona(int idSucursal, long idPersona)
        {

            var datosAfip = await AuthenticatePadronAsync(idSucursal);

            // Crear la solicitud SOAP
            var soapRequest = $@"
                <soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:a13=""http://a13.soap.ws.server.puc.sr/"">
                    <soapenv:Header/>
                    <soapenv:Body>
                        <a13:getPersona>
                            <token>{datosAfip.TokenPadron}</token>
                            <sign>{datosAfip.SignPadron}</sign>
                            <cuitRepresentada>{datosAfip.CuitFactura}</cuitRepresentada>
                            <idPersona>{idPersona}</idPersona>
                        </a13:getPersona>
                    </soapenv:Body>
                </soapenv:Envelope>";

            // Configurar la solicitud HTTP
            string urlWsPadron = _configuration["Afip:urlWsPadron"];
            var content = new StringContent(soapRequest, Encoding.UTF8, "text/xml");
            content.Headers.Add("SOAPAction", "");
            var requestUri = urlWsPadron;// + "/getIdPersona";

            // Enviar la solicitud POST
            HttpResponseMessage response = await _httpClient.PostAsync(requestUri, content);
            //response.EnsureSuccessStatusCode();

            // Leer el contenido de la respuesta
            var responseContent = await response.Content.ReadAsStringAsync();

            // Procesar la respuesta SOAP
            return ParsePersonaResponse(responseContent);
        }

        // Método para procesar la respuesta SOAP
        private PersonaResponse ParsePersonaResponse(string responseXml)
        {
            var personaResponse = new PersonaResponse();

            // Procesar la respuesta XML y llenar el objeto PersonaResponse
                        // Validación de los campos obligatorios en la respuesta
            // Si no se encuentra, lanza una excepción
            if (string.IsNullOrEmpty(responseXml))
            {
                throw new Exception("La respuesta del servicio está vacía o malformada.");
            }

            // Aquí debes realizar el parsing del XML y asignar los valores a personaResponse
            // Considera los campos opcionales y obligatorios según la multiplicidad
            // Ejemplo de procesamiento:
            var xmlDoc = XDocument.Parse(responseXml);

            // Verificar si la respuesta contiene un Fault
            XName namefaultcode = "faultcode";
            XName namefaultstring = "faultstring";
            // Extraer el código de error (faultcode)
            var faultCodeElement = xmlDoc.Descendants(namefaultcode).FirstOrDefault();
            string faultCode = faultCodeElement?.Value ?? string.Empty;

            // Extraer el mensaje de error (faultstring)
            var faultStringElement = xmlDoc.Descendants(namefaultstring).FirstOrDefault();
            string faultString = faultStringElement?.Value ?? string.Empty;
            XName nameFault = "Fault";
            var fault = xmlDoc.Descendants(nameFault).FirstOrDefault();
            if (!String.IsNullOrEmpty(faultString) || !String.IsNullOrEmpty(faultString))
            {
                Console.WriteLine($"Error SOAP - Código: {faultCode}, Mensaje: {faultString}");
                return personaResponse; // Retorna la lista vacía si hay error
            }


            XName Xpersona = "persona";
            var personaElement = xmlDoc.Descendants(Xpersona).FirstOrDefault();
            if (personaElement != null)
            {
                // Extraer datos de la persona
                personaResponse.Persona = new Persona
                {
                    Apellido = ExtractXmlField(personaElement, "apellido"),
                    Nombre = ExtractXmlField(personaElement, "nombre"),
                    RazonSocial = ExtractXmlField(personaElement, "razonSocial"),
                    EstadoClave = ExtractXmlField(personaElement, "estadoClave"),//ValidateEstadoClave(ExtractXmlField(responseXml, "estadoClave")),
                    IdPersona = long.Parse(ExtractXmlField(personaElement, "idPersona")),
                    NumeroDocumento = long.Parse(ExtractXmlField(personaElement, "numeroDocumento")),
                    TipoDocumento = ExtractXmlField(personaElement, "tipoDocumento"),
                    TipoPersona = ExtractXmlField(personaElement, "tipoPersona"),//ValidateTipoPersona(ExtractXmlField(responseXml, "tipoPersona")),
                    FechaNacimiento = DateTime.TryParse(ExtractXmlField(personaElement, "fechaNacimiento"), out DateTime fechaNacimiento)
                    ? fechaNacimiento
                    : (DateTime?)null,
                    // Otras validaciones...
                };
            }
            // Procesar los domicilios (puede haber múltiples)
            XName xDomicilio = "domicilio";
            var domiciliosElement = xmlDoc.Descendants(xDomicilio);
            foreach (var domicilioElem in domiciliosElement)
            {
                personaResponse.Persona.Domicilios.Add(new Domicilio
                {
                    Calle = ExtractXmlField(domicilioElem, "calle"),
                    Numero = ExtractXmlField(domicilioElem, "numero"),
                    CodigoPostal = ExtractXmlField(domicilioElem, "codigoPostal"),
                    TipoDomicilio = ExtractXmlField(domicilioElem, "tipoDomicilio"),//ValidateTipoDomicilio(ExtractXmlField(domicilioXml, "tipoDomicilio")),
                    // Otras validaciones...
                });
            }

            return personaResponse;
        }

        // Métodos auxiliares para extraer campos XML (a ajustar según el parser que uses)
        public static string ExtractXmlField(XElement nodeElement, string xpath)
        {
            try
            {               
                string nodo = string.Empty;
                if (nodeElement != null)
                {
                    XName XNodo = xpath;
                    nodo = (string)nodeElement.Element(XNodo);
                }

                return nodo;
            }
            catch (Exception ex)
            {
                // Manejo de errores, puedes loguear o lanzar una excepción
                Console.WriteLine($"Error extracting XML field: {ex.Message}");
                return string.Empty;
            }
        }
    }
}
