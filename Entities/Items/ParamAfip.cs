using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;


namespace Entities.Items
{
    public partial class ParamAfip
    {
        [Key]
        public int IdSucursal { get; set; }
        public int PuntoDeVenta { get; set; }
        public string CuitFactura { get; set; } = "";
        public string Token { get; set; } = "";
        public string Sign { get; set; } = "";

        [Column(TypeName = "datetime")]
        public DateTime FechaExpiracion { get; set; }

        public string TokenPadron { get; set; } = "";
        public string SignPadron { get; set; } = "";

        [Column(TypeName = "datetime")]
        public DateTime FechaExpiracionPadron { get; set; }

        public string TipoFactura { get; set; } = "";
        public string NombreFantasia { get; set; } = "";    
        public string RazonSocial { get; set; } = "";
        public string Domicilio { get; set; } = "";
        public string CondicionFrenteAlIva { get; set; } = "";

        [Column(TypeName = "datetime")]
        public DateTime FechaInicioActividades { get; set; }

        public long UltimoComprobanteAprobado { get; set; }


    }
}