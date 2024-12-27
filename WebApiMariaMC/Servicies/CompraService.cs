using Entities.Items;
using Entities.RequestModels;
using Logic.ILogic;
using WebApiMariaMC.IServicies;

namespace WebApiMariaMC.Servicies
{
    public class CompraService : ICompraService
    {
        private readonly ICompraLogic _compraLogic;
        public CompraService(ICompraLogic compraLogic)
        {
            _compraLogic = compraLogic;
        }
        public Task<long> RealizarCompra(CompraRequest compra) { return _compraLogic.RealizarCompra(compra); }


    }
}
