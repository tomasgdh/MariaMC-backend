using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.ResponseModels
{
    public class ProductoPrintResponse
    {
        public long IdProducto { get; set; }
        //public string Descripcion { get; set; }
        //public int IdEstado { get; set; }
        //public string EstadoDescripcion { get; set; }
        //public int IdTipoProducto { get; set; } // Categoria
        public string TipoProductoDescripcion { get; set; }
        //public int IdTipoMarca { get; set; }
        public string TipoMarcaDescripicion { get; set; }
        //public int IdTipoTalle { get; set; }
        public string TipoTalleDescripcion { get; set; }
        public decimal PrecioDeVenta { get; set; }
    }
}
