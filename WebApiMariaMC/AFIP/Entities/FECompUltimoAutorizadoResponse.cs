namespace WebApiMariaMC.AFIP.Entities
{
    public class FECompUltimoAutorizadoResponse
    {
        public List<FEUltimoComprobanteAutorizado> comprobante { get; set; } = new List<FEUltimoComprobanteAutorizado>();
        public List<ErrorAFIP> Errors { get; set; } = new List<ErrorAFIP>();
        public List<EventoAFIP> Events { get; set; } = new List<EventoAFIP>();
    }
}
