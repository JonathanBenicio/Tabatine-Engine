using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tabatine.Core.Entities;

namespace Tabatine.Infrastructure.Data.Configurations
{
    public class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
    {
        public void Configure(EntityTypeBuilder<Cliente> builder)
        {
            builder.ToTable("Clientes");
            builder.HasKey(c => c.Id);
            
            builder.HasIndex(c => c.OmieId).IsUnique();
            
            builder.Property(c => c.CnpjCpf).HasMaxLength(20);
            builder.Property(c => c.RazaoSocial).HasMaxLength(255).IsRequired();
        }
    }
}
