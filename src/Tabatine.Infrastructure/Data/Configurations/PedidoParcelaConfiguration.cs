using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tabatine.Core.Entities;

namespace Tabatine.Infrastructure.Data.Configurations
{
    public class PedidoParcelaConfiguration : IEntityTypeConfiguration<PedidoParcela>
    {
        public void Configure(EntityTypeBuilder<PedidoParcela> builder)
        {
            builder.ToTable("pedido_parcelas");
            builder.HasKey(p => p.Id);

            builder.HasOne(p => p.PedidoVenda)
                   .WithMany(pv => pv.Parcelas)
                   .HasForeignKey(p => p.PedidoVendaId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(p => p.ContaCorrente)
                   .WithMany(c => c.Parcelas)
                   .HasForeignKey(p => p.ContaCorrenteId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(p => p.MeioPagamento)
                   .WithMany()
                   .HasForeignKey(p => p.MeioPagamentoId);

            builder.Property(p => p.Valor).HasColumnType("numeric(18,2)");
            builder.Property(p => p.Percentual).HasColumnType("numeric(5,2)");
        }
    }
}
