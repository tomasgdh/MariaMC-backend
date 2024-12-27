using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Entities.Items;

namespace Data.Models.Configurations
{
    public partial class TipoEstadoConfiguration : IEntityTypeConfiguration<TipoEstado>
    {
        public void Configure(EntityTypeBuilder<TipoEstado> entity)
        {
            entity.HasKey(e => e.Id).HasName("PK__TipoEsta__21A386D4E83C17A4");

            entity.Property(e => e.Activo)
            .HasDefaultValueSql("('S')")
            .IsFixedLength();
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.ModifiedDate).HasDefaultValueSql("(getdate())");

            OnConfigurePartial(entity);
        }

        partial void OnConfigurePartial(EntityTypeBuilder<TipoEstado> entity);
    }
}
