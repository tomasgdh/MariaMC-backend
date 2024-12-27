using Entities.Items;
using Entities.RequestModels;
using Entities.ResponseModels;
using Logic.ILogic;
using WebApiMariaMC.IServicies;

namespace WebApiMariaMC.Servicies
{
    public class CierreDeCajaService : ICierreDeCajaService
    {
        private readonly ICierreDeCajaLogic _cierreDeCajaLogic;
        public CierreDeCajaService(ICierreDeCajaLogic cierreDeCajaLogic)
        {
            _cierreDeCajaLogic = cierreDeCajaLogic;
        }
        public Task<CierreDeCajaResponse> CierreDeCaja(CierreDeCajaRequest cierreDeCaja) {
            return _cierreDeCajaLogic.CierreDeCaja(cierreDeCaja); 
        } 
        public Task<CierreDeCajaResponse> CierreDeCajaX(CierreDeCajaRequest cierreDeCaja) {
            return _cierreDeCajaLogic.CierreDeCajaX(cierreDeCaja); 
        }

        public Task<CierreDeCajaResponse> GetCierreDeCaja(long idCierre) {
            return _cierreDeCajaLogic.GetCierreDeCaja(idCierre);
        }        
        public Task<CierreDeCajaResponse> GetUltimoCierreDeCaja() {
            return _cierreDeCajaLogic.GetUltimoCierreDeCaja();
        } 
        public Task<List<CCResponseDetalle>> GetAllCierreDeCaja(int idSucursal) {
            return _cierreDeCajaLogic.GetAllCierreDeCaja(idSucursal);
        }  
        public Task<CCResponse> GetAllCierreDeCajaPaginado(int idSucursal, int pageNumber, int pageSize) {
            return _cierreDeCajaLogic.GetAllCierreDeCajaPaginado(idSucursal,pageNumber,pageSize);
        }
    }
}
