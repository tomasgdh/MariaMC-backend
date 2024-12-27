using Entities.Items;
using Entities.RequestModels;
using Entities.ResponseModels;
using Logic.Session;

namespace Logic.ILogic
{
    public interface IVentaLogic
    {
        Task<long> RealizarVenta(VentaRequest venta);

        //Task<object> GenerarComprobanteYEnviarAlCliente(Int64 idVenta);
        Task<ListadoVentasResponse> GetAllVentasPaginado(int idSucursal, DateTime fechaDesde, DateTime fechaHasta, int pageNumber, int pageSize);

    }
}
