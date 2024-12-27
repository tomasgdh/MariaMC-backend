using Entities.Items;
using Entities.RequestModels;
using Entities.ResponseModels;
using Logic.Session;

namespace Logic.ILogic
{
    public interface ICierreDeCajaLogic
    {
        Task<CierreDeCajaResponse> CierreDeCaja(CierreDeCajaRequest cierreDeCaja);
        Task<CierreDeCajaResponse> CierreDeCajaX(CierreDeCajaRequest cierreDeCaja);
        Task<CierreDeCajaResponse> CalcularDetallesCierreDeCaja(long idCierre, DateTime fechaDesde, DateTime fechaDeCierre, string usuarioCierre);
        Task<CierreDeCajaResponse> GetCierreDeCaja(long idCierre);
        Task<CierreDeCajaResponse> GetUltimoCierreDeCaja();
        Task<List<CCResponseDetalle>> GetAllCierreDeCaja(int idSucursal);
        Task<CCResponse> GetAllCierreDeCajaPaginado(int idSucursal, int pageNumber, int pageSize);

    }
}
