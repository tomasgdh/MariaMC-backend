namespace WebApiMariaMC.AFIP.Entities
{
    public class PersonaResponse
    {
        public Persona Persona { get; set; } = new Persona();
    }

    public class Persona
    {
        public long IdPersona { get; set; }
        public string TipoPersona { get; set; }  
        public long NumeroDocumento { get; set; }
        public string TipoDocumento { get; set; }
        public string TipoClave { get; set; }
        public string EstadoClave { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string RazonSocial { get; set; }
        public DateTime? FechaNacimiento { get; set; }
        public List<Domicilio> Domicilios { get; set; } = new List<Domicilio>();
        // Otros campos según la documentación...
    }

    public class Domicilio
    {
        public string Calle { get; set; }
        public string Numero { get; set; }
        public string CodigoPostal { get; set; }
        public string TipoDomicilio { get; set; }
        // Otros campos...
    }

}
