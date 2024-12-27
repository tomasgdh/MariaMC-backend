namespace WebApiMariaMC.AFIP.Entities
{
    public class FETipoParametroResponse
    {
        public List<TipoParametro> TiposParametro { get; set; } = new List<TipoParametro>();
        public List<ErrorAFIP> Errors { get; set; } = new List<ErrorAFIP>();
        public List<EventoAFIP> Events { get; set; } = new List<EventoAFIP>();
    }
}
