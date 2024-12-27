using Entities.Items;
using Entities.RequestModels;
using Entities.ResponseModels;

namespace WebApiMariaMC.IServicies
{
    public interface IVentaService
    {
        Task<long> RealizarVenta(VentaRequest venta);
        //Task<object> GenerarComprobanteYEnviarAlCliente(Int64 idVenta);
        Task<ListadoVentasResponse> GetAllVentasPaginado(int idSucursal, DateTime fechaDesde, DateTime fechaHasta, int pageNumber, int pageSize);
    }
}
