using System.ComponentModel;

namespace Enumeradores
{
    public enum TipoFormaPago
    {
        [Description("EFECTIVO")]
        EFECTIVO = 1,
        [Description("MP - TRANSFERENCIA")]
        MP_TRANSFERENCIA = 2,
        [Description("MP - QR")]
        MP_QR = 3,
        [Description("MP - DEBITO")]
        MP_DEBITO = 4,
        [Description("MP - CREDITO")]
        MP_CREDITO = 5,
        [Description("CREDITO EN TIENDA")]
        CREDITO_EN_TIENDA = 6,
        [Description("EFECTIVO - 20 % OFF")]
        EFECTIVO_20OFF = 7,
        [Description("RESERVADO")]
        RESERVADO1 = 8,
        [Description("RESERVADO")]
        RESERVADO2 = 9,
        [Description("EFECTIVO - 10 % OFF")]
        EFECTIVO_10OFF = 10,
    }
}