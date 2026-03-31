using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tabatine.Core.Entities;

namespace Tabatine.Infrastructure.Data.Configurations
{
    public class EtapaFaturamentoConfiguration : IEntityTypeConfiguration<EtapaFaturamento>
    {
        public void Configure(EntityTypeBuilder<EtapaFaturamento> builder)
        {
            builder.ToTable("etapas_faturamento");
            builder.HasKey(e => e.Id);
            
            // Usamos o código da etapa dentro da operação como chave natural da Omie
            builder.HasIndex(e => new { e.CodigoOperacao, e.Codigo }).IsUnique();
            
            builder.Property(e => e.Codigo).HasMaxLength(20).IsRequired();
            builder.Property(e => e.Descricao).HasMaxLength(100).IsRequired();
            builder.Property(e => e.DescricaoPadrao).HasMaxLength(100);
            builder.Property(e => e.CodigoOperacao).HasMaxLength(20).IsRequired();
            builder.Property(e => e.DescricaoOperacao).HasMaxLength(100).IsRequired();
        }
    }
}
