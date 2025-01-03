﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
using Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using Entities.Items;


namespace Data.Models.Configurations
{
    public partial class EmpleadoConfiguration : IEntityTypeConfiguration<Empleado>
    {
        public void Configure(EntityTypeBuilder<Empleado> entity)
        {
            entity.HasKey(e => e.IdEmpleado).HasName("PK__Empleado__CE6D8B9E98D5279D");

            entity.Property(e => e.Activo)
            .HasDefaultValueSql("('S')")
            .IsFixedLength();
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.ModifiedDate).HasDefaultValueSql("(getdate())");

            OnConfigurePartial(entity);
        }

        partial void OnConfigurePartial(EntityTypeBuilder<Empleado> entity);
    }
}
