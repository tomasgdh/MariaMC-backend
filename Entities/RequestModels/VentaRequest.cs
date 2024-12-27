
namespace Entities.RequestModels
{
    public class VentaRequest
    {
        public int idSucursal { get; set; }
        public List<dtoProductoVenta> listaDeProductos { get; set; }
        public int idCliente { get; set; }
        public List<dtoMedioDePagoVenta> listaMediosDePago { get; set; }
        public decimal subTotal { get; set; }
        public decimal descuentoValor { get; set; }
        public decimal total { get; set; }
        public int idUsuario { get; set; }

    }
    public class dtoProductoVenta
    {
        public Int64 id { get; set; }
        public decimal price { get; set; }
    }

    public class dtoMedioDePagoVenta
    {
        public int id { get; set; }
        public decimal subTotal { get; set; }
        public decimal descuentoValor { get; set; }
        public decimal total { get; set; }

    }

}