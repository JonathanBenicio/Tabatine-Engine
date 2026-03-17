using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tabatine.Core.Entities;

namespace Tabatine.Infrastructure.Data.Configurations
{
    public class ContaCorrenteConfiguration : IEntityTypeConfiguration<ContaCorrente>
    {
        public void Configure(EntityTypeBuilder<ContaCorrente> builder)
        {
            builder.HasKey(c => c.Id);
            builder.Property(c => c.OmieId).IsRequired();
            builder.HasIndex(c => c.OmieId).IsUnique();

            builder.Property(c => c.Descricao).HasMaxLength(100).IsRequired();
            builder.Property(c => c.CodigoIntegracao).HasMaxLength(50);
            builder.Property(c => c.Tipo).HasMaxLength(20);
        }
    }
}
