namespace WebApiMariaMC.AFIP.Entities
{
    public class ComprobanteData
    {
        public int CbteTipo { get; set; } = 11;
        public int Concepto { get; set; }
        public string DenominacionCliente { get; set; }
        public int DocTipo { get; set; }
        public long DocNro { get; set; }
        public long CbteDesde { get; set; }
        public long CbteHasta { get; set; }
        public string CbteFch { get; set; }
        public double ImpTotal { get; set; }
        //public double ImpTotConc { get; set; } = 0;
        //public double ImpNeto { get; set; } = 0;
        //public double ImpOpEx { get; set; } = 0;
        //public double ImpTrib { get; set; } = 0;
        //public double ImpIVA { get; set; } = 0;
        //public string FchServDesde { get; set; }
        //public string FchServHasta { get; set; }
        //public string FchVtoPago { get; set; }
        public string MonId { get; set; } = "PES";
        public double MonCotiz { get; set; } = 1;
    }
}
