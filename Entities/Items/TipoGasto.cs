﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Entities.Items;

[Table("TiposDeGasto")]
public partial class TipoGasto
{
    [Key]
    [Column("IdTipoDeGasto")]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    [Unicode(false)]
    public string Descripcion { get; set; }

    [Required]
    [StringLength(1)]
    [Unicode(false)]
    public string Activo { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? CreatedDate { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? ModifiedDate { get; set; }

    public int IdUsuario { get; set; }

    [InverseProperty("IdTipoDeGastoNavigation")]
    public virtual ICollection<Gasto> Gasto { get; set; } = new List<Gasto>();

    [ForeignKey("IdUsuario")]
    [InverseProperty("TipoGasto")]
    public virtual Usuario IdUsuarioNavigation { get; set; }
}