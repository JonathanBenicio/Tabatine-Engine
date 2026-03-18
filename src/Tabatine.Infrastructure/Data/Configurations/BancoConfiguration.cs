using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tabatine.Core.Entities;

namespace Tabatine.Infrastructure.Data.Configurations
{
    public class BancoConfiguration : IEntityTypeConfiguration<Banco>
    {
        public void Configure(EntityTypeBuilder<Banco> builder)
        {
            builder.HasKey(b => b.Id);
            builder.HasIndex(b => b.CodigoBanco).IsUnique();
            
            builder.Property(b => b.CodigoBanco).HasMaxLength(10).IsRequired();
            builder.Property(b => b.Nome).HasMaxLength(150).IsRequired();
            builder.Property(b => b.CodigoIspb).HasMaxLength(20);
            builder.Property(b => b.Tipo).HasMaxLength(50);
        }
    }
}
