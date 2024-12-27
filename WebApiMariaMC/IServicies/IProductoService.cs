using Entities.Items;
using Entities.RequestModels;

namespace WebApiMariaMC.IServicies
{
    public interface IProductoService
    {
        Task<object> CargaDeProductosporArchivo(CargarProductosRequest productoRequest);
        Task<object> GetProductoToSell(string codigo);
        Task<object> GetProducto(string codigo);
        Task<object> GuardarProducto(UpdateProductosRequest productoRequest);
        Task<object> GetAllProductosToPrint();
    }
}
