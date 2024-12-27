using Entities.Items;
using Entities.RequestModels;
using Entities.ResponseModels;
using Logic.ILogic;
using Logic.Logic;
using WebApiMariaMC.IServicies;

namespace WebApiMariaMC.Servicies
{
    public class VentaService : IVentaService
    {
        private readonly IVentaLogic _ventaLogic;
        public VentaService(IVentaLogic ventaLogic)
        {
            _ventaLogic = ventaLogic;
        }
        public Task<long> RealizarVenta(VentaRequest venta) { return _ventaLogic.RealizarVenta(venta); }
        public Task<ListadoVentasResponse> GetAllVentasPaginado(int idSucursal, DateTime fechaDesde, DateTime fechaHasta, int pageNumber, int pageSize)
        {
            return _ventaLogic.GetAllVentasPaginado(idSucursal,fechaDesde,fechaHasta, pageNumber, pageSize);
        }
    }
}
