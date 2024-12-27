
namespace Entities.RequestModels
{
    public class ItemTablabasicaRequest
    {
        public int id { get; set; }
        public string descripcion { get; set; }
        public string activo { get; set; }
        public int idUsuario { get; set; }

    }

    public class ItemTBTipoFormaDePagoRequest
    {
        public int id { get; set; }
        public string descripcion { get; set; }
        public decimal descuento { get; set; }
        public string activo { get; set; }
        public int idUsuario { get; set; }

    }

    public class ItemTablabasicaDeleteRequest
    {
        public int id { get; set; }
        public int idUsuario { get; set; }

    }

}