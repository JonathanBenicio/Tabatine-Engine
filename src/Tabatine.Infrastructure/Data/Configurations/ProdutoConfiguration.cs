using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tabatine.Core.Entities;

namespace Tabatine.Infrastructure.Data.Configurations
{
    public class ProdutoConfiguration : IEntityTypeConfiguration<Produto>
    {
        public void Configure(EntityTypeBuilder<Produto> builder)
        {
            builder.ToTable("Produtos");
            builder.HasKey(p => p.Id);
            
            builder.HasIndex(p => p.OmieId).IsUnique();
            
            builder.Property(p => p.CodigoProduto).HasMaxLength(50).IsRequired();
            builder.Property(p => p.Descricao).HasMaxLength(255).IsRequired();
            builder.Property(p => p.PrecoUnitario).HasColumnType("numeric(18,2)");
            builder.Property(p => p.PesoLiquido).HasColumnType("numeric(18,4)");
            builder.Property(p => p.PesoBruto).HasColumnType("numeric(18,4)");
        }
    }
}
