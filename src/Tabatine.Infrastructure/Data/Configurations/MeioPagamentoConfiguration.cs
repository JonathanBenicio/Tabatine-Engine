using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tabatine.Core.Entities;

namespace Tabatine.Infrastructure.Data.Configurations
{
    public class MeioPagamentoConfiguration : IEntityTypeConfiguration<MeioPagamento>
    {
        public void Configure(EntityTypeBuilder<MeioPagamento> builder)
        {
            builder.ToTable("meios_pagamento");

            builder.HasKey(m => m.Id);

            builder.Property(m => m.OmieId)
                .IsRequired();

            builder.HasIndex(m => m.OmieId)
                .IsUnique();

            builder.Property(m => m.Codigo)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(m => m.Descricao)
                .IsRequired()
                .HasMaxLength(150);
        }
    }
}
