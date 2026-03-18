using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tabatine.Core.Entities;

namespace Tabatine.Infrastructure.Data.Configurations
{
    public class CaracteristicaConfiguration : IEntityTypeConfiguration<Caracteristica>
    {
        public void Configure(EntityTypeBuilder<Caracteristica> builder)
        {
            builder.ToTable("Caracteristicas");

            builder.HasKey(c => c.Id);

            builder.Property(c => c.OmieId)
                .IsRequired();

            builder.HasIndex(c => c.OmieId)
                .IsUnique();

            builder.Property(c => c.Nome)
                .IsRequired()
                .HasMaxLength(100);

            builder.HasMany(c => c.Valores)
                .WithOne(v => v.Caracteristica)
                .HasForeignKey(v => v.CaracteristicaId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class CaracteristicaValorConfiguration : IEntityTypeConfiguration<CaracteristicaValor>
    {
        public void Configure(EntityTypeBuilder<CaracteristicaValor> builder)
        {
            builder.ToTable("CaracteristicaValores");

            builder.HasKey(v => v.Id);

            builder.Property(v => v.Valor)
                .IsRequired()
                .HasMaxLength(255);
        }
    }
}
