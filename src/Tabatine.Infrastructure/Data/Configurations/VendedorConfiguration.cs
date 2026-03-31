using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tabatine.Core.Entities;

namespace Tabatine.Infrastructure.Data.Configurations
{
    public class VendedorConfiguration : IEntityTypeConfiguration<Vendedor>
    {
        public void Configure(EntityTypeBuilder<Vendedor> builder)
        {
            builder.ToTable("vendedores");
            builder.HasKey(v => v.Id);
            builder.Property(v => v.OmieId).IsRequired();
            builder.HasIndex(v => v.OmieId).IsUnique();

            builder.Property(v => v.Nome).HasMaxLength(150).IsRequired();
            builder.Property(v => v.Email).HasMaxLength(100);
            builder.Property(v => v.Comissao).HasPrecision(18, 2);
        }
    }
}
