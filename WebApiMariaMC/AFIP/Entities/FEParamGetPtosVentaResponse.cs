namespace WebApiMariaMC.AFIP.Entities
{
    public class FEParamGetPtosVentaResponse
    {
        public List<TipoPuntoDeVenta> TipospuntoDeVenta { get; set; } = new List<TipoPuntoDeVenta>();
        public List<ErrorAFIP> Errors { get; set; } = new List<ErrorAFIP>();
        public List<EventoAFIP> Events { get; set; } = new List<EventoAFIP>();
    }
}
