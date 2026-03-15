using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tabatine.Core.Entities;

namespace Tabatine.Infrastructure.Data.Configurations
{
    public class NotaFiscalConfiguration : IEntityTypeConfiguration<NotaFiscal>
    {
        public void Configure(EntityTypeBuilder<NotaFiscal> builder)
        {
            builder.ToTable("NotasFiscais");
            builder.HasKey(n => n.Id);
            builder.HasIndex(n => n.OmieId).IsUnique();
            
            builder.Property(n => n.NumeroNf).HasMaxLength(50);
            builder.Property(n => n.ChaveAcesso).HasMaxLength(100);
            
            builder.HasOne(n => n.Cliente)
                   .WithMany()
                   .HasForeignKey(n => n.ClienteId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(n => n.PedidoVenda)
                   .WithMany()
                   .HasForeignKey(n => n.PedidoVendaId)
                   .OnDelete(DeleteBehavior.SetNull);

            builder.Property(n => n.ValorTotal)
                   .HasColumnType("numeric(18,2)");
        }
    }
}
