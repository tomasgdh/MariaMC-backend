﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Entities.Items;

namespace Data.Models.Configurations
{
    public partial class TiposDeIngresoConfiguration : IEntityTypeConfiguration<TipoIngreso>
    {
        public void Configure(EntityTypeBuilder<TipoIngreso> entity)
        {
            entity.HasKey(e => e.Id).HasName("PK_TiposDeIngreso");

            entity.Property(e => e.Activo)
            .HasDefaultValueSql("('S')")
            .IsFixedLength();
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.ModifiedDate).HasDefaultValueSql("(getdate())");

            OnConfigurePartial(entity);
        }

        partial void OnConfigurePartial(EntityTypeBuilder<TipoIngreso> entity);
    }
}