using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tabatine.Core.Entities;

namespace Tabatine.Infrastructure.Data.Configurations
{
    public class ProdutoEstoqueConfiguration : IEntityTypeConfiguration<ProdutoEstoque>
    {
        public void Configure(EntityTypeBuilder<ProdutoEstoque> builder)
        {
            builder.ToTable("estoque_produtos");
            builder.HasKey(e => e.Id);
            
            builder.HasIndex(e => new { e.ProdutoId, e.LocalEstoqueId }).IsUnique();
            
            builder.Property(e => e.Saldo).HasColumnType("numeric(18,4)");
            builder.Property(e => e.Reservado).HasColumnType("numeric(18,4)");
            builder.Property(e => e.Pendente).HasColumnType("numeric(18,4)");
            builder.Property(e => e.Fisico).HasColumnType("numeric(18,4)");
            builder.Property(e => e.Cmc).HasColumnType("numeric(18,4)");
            builder.Property(e => e.EstoqueMinimo).HasColumnType("numeric(18,4)");

            builder.HasOne(e => e.Produto)
                .WithMany()
                .HasForeignKey(e => e.ProdutoId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(e => e.LocalEstoque)
                .WithMany()
                .HasForeignKey(e => e.LocalEstoqueId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
