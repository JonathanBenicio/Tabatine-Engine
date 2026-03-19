using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tabatine.Core.Entities;

namespace Tabatine.Infrastructure.Data.Configurations
{
    public class ItemNotaFiscalConfiguration : IEntityTypeConfiguration<ItemNotaFiscal>
    {
        public void Configure(EntityTypeBuilder<ItemNotaFiscal> builder)
        {
            builder.ToTable("ItensNotaFiscal");
            builder.HasKey(i => i.Id);

            builder.HasOne(i => i.NotaFiscal)
                   .WithMany(nf => nf.Itens)
                   .HasForeignKey(i => i.NotaFiscalId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(i => i.Produto)
                   .WithMany(p => p.ItensNotaFiscal)
                   .HasForeignKey(i => i.ProdutoId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.Property(i => i.Quantidade).HasColumnType("numeric(18,4)");
            builder.Property(i => i.ValorUnitario).HasColumnType("numeric(18,2)");
            builder.Property(i => i.ValorTotal).HasColumnType("numeric(18,2)");
        }
    }
}
