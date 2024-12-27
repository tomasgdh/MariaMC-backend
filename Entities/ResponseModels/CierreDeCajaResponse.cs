using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.ResponseModels
{
    public class CierreDeCajaResponse
    {
        public long IdCierre { get; set; } = -1;
        public string FechaDesde { get; set; }
        public string FechaHasta { get; set; }
        public string UsuarioCierre { get; set; }
        public decimal TotalEfectivo { get; set; }
        public decimal TotalMP { get; set; }
        public decimal EnCaja { get; set; }
        public decimal AperturaEfCaja { get; set; }
        public decimal CierreEfCaja { get; set; }
        public decimal EfectivoAGuardar { get; set; }
        public List<DetalleTransaccion> Detalle { get; set; }
    }
    public class DetalleTransaccion
    {
        public string FechaHora { get; set; }
        public long IdMovimiento { get; set; }
        public string TipoMovimiento { get; set; }
        public string FormaDePago { get; set; }
        public decimal Importe { get; set; }
    }

    public class CCResponseDetalle
    {
        public long IdCierre { get; set; } = -1;
        public string FechaDesde { get; set; }
        public string FechaHasta { get; set; }
        public string Usuario { get; set; }
        public int CantidadDeVentas { get; set; }
        public int CantidadDePrendas { get; set; }
        public decimal TotalEfectivo { get; set; }
        public decimal TotalMP { get; set; }
    }

    public class CCResponse
    {
        public long totalDeRegistros { get; set; }

        public List<CCResponseDetalle> Cierres { get; set; }

    }

}
