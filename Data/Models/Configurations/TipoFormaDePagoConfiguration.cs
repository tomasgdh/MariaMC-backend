﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Entities.Items;


namespace Data.Models.Configurations
{
    public partial class TipoFormaDePagoConfiguration : IEntityTypeConfiguration<TipoFormaDePago>
    {
        public void Configure(EntityTypeBuilder<TipoFormaDePago> entity)
        {
            entity.HasKey(e => e.Id).HasName("PK__TipoForm__F35DA87EE9DF4DAA");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Activo)
            .HasDefaultValueSql("('S')")
            .IsFixedLength();
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.ModifiedDate).HasDefaultValueSql("(getdate())");

            OnConfigurePartial(entity);
        }

        partial void OnConfigurePartial(EntityTypeBuilder<TipoFormaDePago> entity);
    }
}