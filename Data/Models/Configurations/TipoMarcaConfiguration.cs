using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Entities.Items;

namespace Data.Models.Configurations
{
    public partial class TipoMarcaConfiguration : IEntityTypeConfiguration<TipoMarca>
    {
        public void Configure(EntityTypeBuilder<TipoMarca> entity)
        {
            entity.HasKey(e => e.Id).HasName("PK_TipoMarca");

            entity.Property(e => e.Activo)
            .HasDefaultValueSql("('S')")
            .IsFixedLength();
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.ModifiedDate).HasDefaultValueSql("(getdate())");

            OnConfigurePartial(entity);
        }

        partial void OnConfigurePartial(EntityTypeBuilder<TipoMarca> entity);
    }
}
