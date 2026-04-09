using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tabatine.Core.Entities;

namespace Tabatine.Infrastructure.Data.Configurations;

public class TituloReceberConfiguration : IEntityTypeConfiguration<TituloReceber>
{
    public void Configure(EntityTypeBuilder<TituloReceber> builder)
    {
        builder.ToTable("titulos_receber");

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
        builder.Property(t => t.ValorRecebido).HasPrecision(15, 2);
        builder.Property(t => t.ValorSaldo).HasPrecision(15, 2);

        builder.Property(t => t.CreatedAt).HasDefaultValueSql("now()");

        builder.HasOne(t => t.Cliente)
               .WithMany()
               .HasForeignKey(t => t.ClienteId)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(t => t.Vendedor)
               .WithMany()
               .HasForeignKey(t => t.VendedorId)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(t => t.ContaCorrente)
               .WithMany()
               .HasForeignKey(t => t.ContaCorrenteId)
               .OnDelete(DeleteBehavior.SetNull);
    }
}
