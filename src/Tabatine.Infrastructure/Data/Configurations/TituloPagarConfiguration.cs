using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tabatine.Core.Entities;

namespace Tabatine.Infrastructure.Data.Configurations;

public class TituloPagarConfiguration : IEntityTypeConfiguration<TituloPagar>
{
    public void Configure(EntityTypeBuilder<TituloPagar> builder)
    {
        builder.ToTable("titulos_pagar");

        builder.HasKey(t => t.Id);
        builder.HasIndex(t => t.OmieId).IsUnique();
        builder.HasIndex(t => t.ClienteId);
        builder.HasIndex(t => t.DataVencimento);
        builder.HasIndex(t => t.StatusTitulo);

        builder.Property(t => t.NumeroDocumento).HasMaxLength(100).IsRequired();
        builder.Property(t => t.NumeroParcela).HasMaxLength(20);
        builder.Property(t => t.NumeroPedido).HasMaxLength(50);
        builder.Property(t => t.StatusTitulo).HasMaxLength(30);
        builder.Property(t => t.CodigoCategoria).HasMaxLength(50);
        builder.Property(t => t.Observacao).HasColumnType("text");

        builder.Property(t => t.ValorDocumento).HasPrecision(15, 2);
        builder.Property(t => t.ValorPago).HasPrecision(15, 2);
        builder.Property(t => t.ValorSaldo).HasPrecision(15, 2);

        builder.Property(t => t.CreatedAt).HasDefaultValueSql("now()");

        // FK para Cliente (que no cadastro unificado Omie pode ser fornecedor)
        builder.HasOne(t => t.Cliente)
               .WithMany()
               .HasForeignKey(t => t.ClienteId)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(t => t.ContaCorrente)
               .WithMany()
               .HasForeignKey(t => t.ContaCorrenteId)
               .OnDelete(DeleteBehavior.SetNull);
    }
}
