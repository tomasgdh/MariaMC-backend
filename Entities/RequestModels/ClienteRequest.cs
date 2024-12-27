namespace Entities.RequestModels
{
    public class ClienteRequest
    {
        public string nombre { get; set; }
        public string apellido { get; set; }
        public string mail { get; set; }
        public int idTipoDocumento { get; set; }
        public Int64 nroDocumento { get; set; } 
        public int idUsuario { get; set; } 
    }
}
