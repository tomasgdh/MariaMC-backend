﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
using Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using Entities.Items;


namespace Data.Models.Configurations
{
    public partial class CompraDetalleConfiguration : IEntityTypeConfiguration<CompraDetalle>
    {
        public void Configure(EntityTypeBuilder<CompraDetalle> entity)
        {
            entity.HasKey(e => new { e.IdCompra, e.IdProducto }).HasName("PK_CompraDetalle");

            entity.Property(e => e.Cantidad).HasDefaultValueSql("((1))");

            entity.HasOne(d => d.IdCompraNavigation).WithMany(p => p.CompraDetalles)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("FK_CompraDetalle_Compra");

            entity.HasOne(d => d.IdProductoNavigation).WithMany(p => p.CompraDetalles)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("FK_CompraDetalle_Producto");

            OnConfigurePartial(entity);
        }

        partial void OnConfigurePartial(EntityTypeBuilder<CompraDetalle> entity);
    }
}