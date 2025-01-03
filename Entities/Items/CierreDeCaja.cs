﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.Items;

public partial class CierreDeCaja
{
    [Key]
    public long IdCierre { get; set; }
    public int IdSucursal { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime FechaDesde { get; set; }   
    
    [Column(TypeName = "datetime")]
    public DateTime Fechahasta { get; set; }

    public int CantidadDeVentas { get; set; }
    public int CantidadDePrendas { get; set; }
    public int IdUsuario { get; set; }

    [Column(TypeName = "numeric(18, 2)")]
    public decimal TotalEfectivo { get; set; }

    [Column(TypeName = "numeric(18, 2)")]
    public decimal TotalMp { get; set; }    
    
    [Column(TypeName = "numeric(18, 2)")]
    public decimal AperturaEfCaja { get; set; }    
    
    [Column(TypeName = "numeric(18, 2)")]
    public decimal CierreEfCaja { get; set; }

    [Column(TypeName = "numeric(18, 2)")]
    public decimal EfectivoAGuardar { get; set; }   
    [Column(TypeName = "numeric(18, 2)")]
    public decimal EnCaja { get; set; }

    [InverseProperty("IdCierreNavigation")]
    public virtual ICollection<CuentaCorriente> CuentaCorrientes { get; set; } = new List<CuentaCorriente>();    
    
    [InverseProperty("IdCierreNavigation")]
    public virtual ICollection<CierreDeCajaDetalle> CierreDeCajaDetalle { get; set; } = new List<CierreDeCajaDetalle>();

    [ForeignKey("IdSucursal")]
    [InverseProperty("CierreDeCaja")]
    public virtual Sucursal IdSucursalNavigation { get; set; }

    [ForeignKey("IdUsuario")]
    [InverseProperty("CierreDeCaja")]
    public virtual Usuario IdUsuarioNavigation { get; set; }
}