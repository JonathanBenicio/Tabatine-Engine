using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tabatine.Core.Entities;

namespace Tabatine.Infrastructure.Data.Configurations
{
    public class NotaFiscalConfiguration : IEntityTypeConfiguration<NotaFiscal>
    {
        public void Configure(EntityTypeBuilder<NotaFiscal> builder)
        {
            builder.ToTable("notas_fiscais");
            builder.HasKey(n => n.Id);
            builder.HasIndex(n => n.OmieId).IsUnique();
            
            builder.Property(n => n.NumeroNf).HasMaxLength(50);
            builder.Property(n => n.ChaveAcesso).HasMaxLength(100);
            builder.Property(n => n.NaturezaOperacao).HasMaxLength(200);
            builder.Property(n => n.Serie).HasMaxLength(20);
            
            builder.HasOne(n => n.Cliente)
                   .WithMany(c => c.NotasFiscais)
                   .HasForeignKey(n => n.ClienteId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(n => n.PedidoVenda)
                   .WithMany()
                   .HasForeignKey(n => n.PedidoVendaId)
                   .OnDelete(DeleteBehavior.SetNull);

            builder.Property(n => n.IcmsValor)
               .HasColumnType("numeric(18,2)");

            builder.Property(n => n.ValorIbs)
                   .HasColumnType("numeric(18,2)");

            builder.Property(n => n.ValorCbs)
                   .HasColumnType("numeric(18,2)");

            builder.Property(n => n.ValorTotal)
                   .HasColumnType("numeric(18,2)");

            builder.Property(n => n.ValorIss).HasColumnType("numeric(18,2)");
            builder.Property(n => n.ValorIr).HasColumnType("numeric(18,2)");
            builder.Property(n => n.ValorCsll).HasColumnType("numeric(18,2)");
            builder.Property(n => n.ValorPisRetido).HasColumnType("numeric(18,2)");
            builder.Property(n => n.ValorCofinsRetido).HasColumnType("numeric(18,2)");

            builder.HasOne(n => n.Vendedor)
                   .WithMany(v => v.NotasFiscais)
                   .HasForeignKey(n => n.VendedorId)
                   .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(n => n.ContaCorrente)
                   .WithMany(c => c.NotasFiscais)
                   .HasForeignKey(n => n.ContaCorrenteId)
                   .OnDelete(DeleteBehavior.SetNull);

            builder.Property(n => n.LinkDanfe).HasMaxLength(500);
        }
    }
}
