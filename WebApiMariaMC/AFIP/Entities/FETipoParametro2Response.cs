namespace WebApiMariaMC.AFIP.Entities
{
    public class FETipoParametro2Response
    {
        public List<TipoParametro2> TiposParametro { get; set; } = new List<TipoParametro2>();
        public List<ErrorAFIP> Errors { get; set; } = new List<ErrorAFIP>();
        public List<EventoAFIP> Events { get; set; } = new List<EventoAFIP>();
    }
}
