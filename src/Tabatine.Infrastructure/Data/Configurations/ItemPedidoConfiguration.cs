using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tabatine.Core.Entities;

namespace Tabatine.Infrastructure.Data.Configurations
{
    public class ItemPedidoConfiguration : IEntityTypeConfiguration<ItemPedido>
    {
        public void Configure(EntityTypeBuilder<ItemPedido> builder)
        {
            builder.ToTable("ItensPedido");
            builder.HasKey(i => i.Id);
            
            // Note: If Omie provides an ID for items, use HasIndex(p => p.OmieId).IsUnique();
            // Assuming OmieId can be 0 or null if they don't have one, but we inherited from OmieEntityBase
            
            builder.HasOne(i => i.PedidoVenda)
                   .WithMany(p => p.Itens)
                   .HasForeignKey(i => i.PedidoVendaId)
                   .OnDelete(DeleteBehavior.Cascade);
                   
            builder.HasOne(i => i.Produto)
                   .WithMany()
                   .HasForeignKey(i => i.ProdutoId)
                   .OnDelete(DeleteBehavior.Restrict);
                   
            builder.Property(i => i.ValorUnitario).HasColumnType("numeric(18,2)");
            builder.Property(i => i.ValorTotal).HasColumnType("numeric(18,2)");
        }
    }
}
