using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tabatine.Core.Entities;

namespace Tabatine.Infrastructure.Data.Configurations
{
    public class NotaFiscalTituloConfiguration : IEntityTypeConfiguration<NotaFiscalTitulo>
    {
        public void Configure(EntityTypeBuilder<NotaFiscalTitulo> builder)
        {
            builder.ToTable("NotaFiscalTitulos");
            builder.HasKey(t => t.Id);

            builder.HasOne(t => t.NotaFiscal)
                   .WithMany(nf => nf.Titulos)
                   .HasForeignKey(t => t.NotaFiscalId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(t => t.ContaCorrente)
                   .WithMany()
                   .HasForeignKey(t => t.ContaCorrenteId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(t => t.Vendedor)
                   .WithMany()
                   .HasForeignKey(t => t.VendedorId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.Property(t => t.Valor).HasColumnType("numeric(18,2)");
        }
    }
}
