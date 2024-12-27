using Entities.Items;
using Entities.RequestModels;
using Logic.ILogic;
using WebApiMariaMC.IServicies;

namespace WebApiMariaMC.Servicies
{
    public class ProductoService : IProductoService
    {
        private readonly IProductoLogic _productoLogic;
        public ProductoService(IProductoLogic productoLogic)
        {
            _productoLogic = productoLogic;
        }
        public Task<object> CargaDeProductosporArchivo(CargarProductosRequest productoRequest) {
            return _productoLogic.CargaDeProductosporArchivo(productoRequest); 
        }
        public Task<object> GetProductoToSell(string codigo) { return _productoLogic.GetProductoToSell(codigo); }
        public Task<object> GetProducto(string codigo) { return _productoLogic.GetProducto(codigo); }

        public Task<object> GuardarProducto(UpdateProductosRequest productoRequest) {
            return _productoLogic.GuardarProducto(productoRequest);
        }

        public Task<object> GetAllProductosToPrint() {
            return _productoLogic.GetAllProductosToPrint();
        }


    }
}
