using Entities.Items;
using Entities.RequestModels;
using Logic.Session;

namespace Logic.ILogic
{
    public interface IProductoLogic
    {
        Task<object> CargaDeProductosporArchivo(CargarProductosRequest productoRequest);
        Task<object> GetProductoToSell(string codigo);
        Task<object> GetProducto(string codigo);
        Task<object> GuardarProducto(UpdateProductosRequest productoRequest);
        Task<object> GetAllProductosToPrint();
    }
}
