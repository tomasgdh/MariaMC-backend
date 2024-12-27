using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Entities.Items;

namespace Data.Models.Configurations
{
    public partial class TablaDeConfiguracionConfiguration : IEntityTypeConfiguration<TablaDeConfiguracion>
    {
        public void Configure(EntityTypeBuilder<TablaDeConfiguracion> entity)
        {
            entity.HasKey(e => e.IdConfiguracion).HasName("PK_Configuracion");

            entity.HasOne(d => d.IdSucursalNavigation).WithMany(p => p.TablaDeConfiguracion)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("FK_Configuracion_Sucursales");

            OnConfigurePartial(entity);
        }

        partial void OnConfigurePartial(EntityTypeBuilder<TablaDeConfiguracion> entity);
    }
}
