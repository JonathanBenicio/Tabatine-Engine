using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tabatine.Core.Entities;

namespace Tabatine.Infrastructure.Data.Configurations
{
    public class ProdutoCaracteristicaConfiguration : IEntityTypeConfiguration<ProdutoCaracteristica>
    {
        public void Configure(EntityTypeBuilder<ProdutoCaracteristica> builder)
        {
            builder.ToTable("ProdutoCaracteristicas");

            builder.HasKey(pc => pc.Id);

            builder.HasOne(pc => pc.Produto)
                   .WithMany(p => p.Caracteristicas)
                   .HasForeignKey(pc => pc.ProdutoId);

            builder.HasOne(pc => pc.Caracteristica)
                   .WithMany()
                   .HasForeignKey(pc => pc.CaracteristicaId);

            builder.HasOne(pc => pc.CaracteristicaValor)
                   .WithMany()
                   .HasForeignKey(pc => pc.CaracteristicaValorId);
        }
    }
}
