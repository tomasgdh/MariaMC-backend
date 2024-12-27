using Entities.Items;
using Entities.RequestModels;

namespace WebApiMariaMC.IServicies
{
    public interface ICompraService
    {
        Task<long> RealizarCompra(CompraRequest venta);
    }
}
