﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Entities.Items;

namespace Data.Models.Configurations
{
    public partial class IngresoConfiguration : IEntityTypeConfiguration<Ingreso>
    {
        public void Configure(EntityTypeBuilder<Ingreso> entity)
        {
            entity.HasKey(e => e.Id).HasName("PK_Ingreso");

            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.ModifiedDate).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.IdSucursalNavigation).WithMany(p => p.Ingreso)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("FK_Ingreso_Sucursal");

            entity.HasOne(d => d.IdTipoDeIngresoNavigation).WithMany(p => p.Ingreso)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("FK_Ingreso_TipoDeIngreso");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.Ingreso)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("FK_Ingreso_Usuario");

            entity.HasOne(d => d.IdTipoFormaPagoNavigation).WithMany(p => p.Ingreso)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("FK_Ingreso_FormaPago");

            OnConfigurePartial(entity);
        }

        partial void OnConfigurePartial(EntityTypeBuilder<Ingreso> entity);
    }
}