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
            builder.Property(c => c.Cep).HasMaxLength(10);
            builder.Property(c => c.Estado).HasMaxLength(2);
            builder.Property(c => c.InscricaoEstadual).HasMaxLength(50);
            builder.Property(c => c.InscricaoMunicipal).HasMaxLength(50);
            builder.Property(c => c.Endereco).HasMaxLength(255);
            builder.Property(c => c.Bairro).HasMaxLength(100);
        }
    }
}
