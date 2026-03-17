using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tabatine.Core.Entities;

namespace Tabatine.Infrastructure.Data.Configurations
{
    public class PedidoParcelaConfiguration : IEntityTypeConfiguration<PedidoParcela>
    {
        public void Configure(EntityTypeBuilder<PedidoParcela> builder)
        {
            builder.ToTable("PedidoParcelas");
            builder.HasKey(p => p.Id);

            builder.HasOne(p => p.PedidoVenda)
                   .WithMany(pv => pv.Parcelas)
                   .HasForeignKey(p => p.PedidoVendaId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.Property(p => p.Valor).HasColumnType("numeric(18,2)");
            builder.Property(p => p.Percentual).HasColumnType("numeric(5,2)");
        }
    }
}
