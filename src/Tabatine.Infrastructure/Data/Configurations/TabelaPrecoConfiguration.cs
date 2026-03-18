using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tabatine.Core.Entities;

namespace Tabatine.Infrastructure.Data.Configurations
{
    public class TabelaPrecoConfiguration : IEntityTypeConfiguration<TabelaPreco>
    {
        public void Configure(EntityTypeBuilder<TabelaPreco> builder)
        {
            builder.ToTable("TabelasPreco");

            builder.HasKey(t => t.Id);

            builder.Property(t => t.OmieId)
                .IsRequired();

            builder.HasIndex(t => t.OmieId)
                .IsUnique();

            builder.Property(t => t.Nome)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(t => t.Codigo)
                .HasMaxLength(20);

            builder.HasMany(t => t.Itens)
                .WithOne(i => i.TabelaPreco)
                .HasForeignKey(i => i.TabelaPrecoId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class TabelaPrecoItemConfiguration : IEntityTypeConfiguration<TabelaPrecoItem>
    {
        public void Configure(EntityTypeBuilder<TabelaPrecoItem> builder)
        {
            builder.ToTable("TabelaPrecoItens");

            builder.HasKey(i => i.Id);

            builder.Property(i => i.Valor)
                .HasPrecision(18, 2);

            builder.HasOne(i => i.Produto)
                .WithMany()
                .HasForeignKey(i => i.ProdutoId);
        }
    }
}
