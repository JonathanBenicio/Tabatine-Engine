using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tabatine.Core.Entities;

namespace Tabatine.Infrastructure.Data.Configurations
{
    public class PedidoVendaConfiguration : IEntityTypeConfiguration<PedidoVenda>
    {
        public void Configure(EntityTypeBuilder<PedidoVenda> builder)
        {
            builder.ToTable("PedidosVenda");
            builder.HasKey(p => p.Id);
            builder.HasIndex(p => p.OmieId).IsUnique();
            
            builder.HasOne(p => p.Cliente)
                   .WithMany(c => c.Pedidos)
                   .HasForeignKey(p => p.ClienteId)
                   .OnDelete(DeleteBehavior.Restrict);
                   
            builder.Property(p => p.ValorTotal)
                   .HasColumnType("numeric(18,2)");

            builder.Property(p => p.ValorFrete)
                   .HasColumnType("numeric(18,2)");
        }
    }
}
