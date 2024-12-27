using Entities.Items;
using System.Numerics;

namespace Entities.RequestModels
{
    public class CargarProductosRequest
    {
        public List<ProductoDto> Productos { get; set; }
        public int IdUsuario { get; set; }
    }

    public class UpdateProductosRequest
    {
        public ProductoDto producto { get; set; }
        public int IdUsuario { get; set; }
    }

    public class ProductoDto
    {
        public long Id { get; set; }
        public int IdEstado { get; set; }
        //public string Descripcion { get; set; }
        public decimal PrecioDeCompra { get; set; }
        public decimal PrecioDeVenta { get; set; }
        public int IdTipoProducto { get; set; }
        public int IdTipoTalle { get; set; }
        public int IdTipoMarca { get; set; }
    }
}