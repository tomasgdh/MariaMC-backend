
namespace Entities.RequestModels
{
    public class ItemMovimientoDeCajaRequest
    {
        public long id { get; set; }
        public int idSucursal { get; set; }
        public string descripcion { get; set; }
        public int idTipoMovimiento { get; set; }
        public int idTipoFormaDePago { get; set; }
        public decimal importe { get; set; }
        public int idUsuario { get; set; }

    }


    public class ItemMovimientoDeCajaDeleteRequest
    {
        public int id { get; set; }
        public int idUsuario { get; set; }

    }

}