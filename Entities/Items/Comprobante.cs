using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.Items;

[Table("Comprobante")]
public class Comprobante
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long IdComprobante { get; set; }

    public int IdSucursal { get; set; }
    
    public int NroPuntoVenta { get; set; }

    public long NroComprobante { get; set; }

    public DateTime Fecha { get; set; }

    [Required]
    [Column(TypeName = "char(1)")]
    public char Tipo { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal Total { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string FacturaHtml { get; set; }

    public DateTime? CreatedDate { get; set; } = DateTime.Now;

    public int IdUsuario { get; set; }

    [ForeignKey("IdSucursal")]
    [InverseProperty("Comprobantes")]
    public virtual Sucursal SucursalNavigation { get; set; }
    
    [ForeignKey("IdUsuario")]
    [InverseProperty("Comprobantes")]
    public virtual Usuario UsuarioNavigation { get; set; }

    [InverseProperty("IdComprobanteNavigation")]
    public virtual ICollection<Venta> Venta { get; set; } = new List<Venta>();
}
