using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.ResponseModels
{
    public class VentaResultado
    {
        public long IdVenta { get; set; }
        public int IdSucursal { get; set; }
        public string FechaVenta { get; set; }
        public decimal TotalDeVenta { get; set; }
        public string Comprobante { get; set; }
        public string FacturaHtml { get; set; }
        public string Cliente { get; set; }
        public string NroDocumento { get; set; }
    }

    public class ListadoVentasResponse
    {
        public long totalDeRegistros { get; set; }

        public List<VentaResultado> ventas { get; set; }

    }
}
