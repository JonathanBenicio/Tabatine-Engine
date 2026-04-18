using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tabatine.Core.Entities;

namespace Tabatine.Infrastructure.Data.Configurations
{
    public class LocalEstoqueConfiguration : IEntityTypeConfiguration<LocalEstoque>
    {
        public void Configure(EntityTypeBuilder<LocalEstoque> builder)
        {
            builder.ToTable("estoque_locais");
            builder.HasKey(e => e.Id);
            
            builder.HasIndex(e => e.OmieId).IsUnique();
            
            builder.Property(e => e.Codigo).HasMaxLength(20).IsRequired();
            builder.Property(e => e.Descricao).HasMaxLength(100).IsRequired();
            builder.Property(e => e.Tipo).HasMaxLength(1);
        }
    }
}
