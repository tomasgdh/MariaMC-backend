using Entities.Items;
using Entities.RequestModels;
using Logic.Session;

namespace Logic.ILogic
{
    public interface ICompraLogic
    {
        Task<long> RealizarCompra(CompraRequest venta);

    }
}
