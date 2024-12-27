using System.ComponentModel;

namespace Enumeradores
{
    public enum TipoEstadoEnum
    {
        [Description("Ingresado")]
        Ingresado = 1,
        [Description("Retirado para limpiar")]
        Retirado_para_limpiar = 2,
        [Description("En Stock")]
        En_Stock = 3,
        [Description("Para la venta")]
        Para_la_venta = 4,
        [Description("Vendido")]
        Vendido = 5,
        [Description("Señado")]
        Señado = 6,
        [Description("Expiracion de Seña")]
        Expiracion_de_Seña = 7,
        [Description("Retiro empleadas")]
        Retiro_empleadas = 8
    }
}