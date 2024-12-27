
namespace Entities.RequestModels
{
    public class CompraRequest
    {

        public int idSucursal { get; set; }
        public List<dtoProductoCompra> listaDeProductos { get; set; }
        public int idCliente { get; set; }
        public List<dtoMedioDePagoCompra> listaMediosDePago { get; set; }
        public decimal totalEfectivo { get; set; }
        public decimal totalCredito { get; set; }
        public int idUsuario { get; set; }

    }
    public class dtoProductoCompra
    {
        public int idCategoria { get; set; }
        public int idMarca { get; set; }
        public int idTalle { get; set; }
        //public string descripcion { get; set; }
        public decimal ValorCompra { get; set; }
        public decimal ValorCreditoEnTienda { get; set; }
        public decimal ValorVentaSugerido { get; set; }

    }

    public class dtoMedioDePagoCompra
    {
        public int id { get; set; }
        public decimal total { get; set; }

    }
}