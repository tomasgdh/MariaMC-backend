using Entities.Items;
using Entities.RequestModels;
using Entities.ResponseModels;

namespace WebApiMariaMC.IServicies
{
    public interface ICierreDeCajaService
    {
        Task<CierreDeCajaResponse> CierreDeCaja(CierreDeCajaRequest cierreDeCaja);
        Task<CierreDeCajaResponse> CierreDeCajaX(CierreDeCajaRequest cierreDeCaja);
        Task<CierreDeCajaResponse> GetCierreDeCaja(long idCierre);
        Task<CierreDeCajaResponse> GetUltimoCierreDeCaja();
        Task<List<CCResponseDetalle>> GetAllCierreDeCaja(int idSucursal);
        Task<CCResponse> GetAllCierreDeCajaPaginado(int idSucursal, int pageNumber, int pageSize);
    }
}
