using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tabatine.Core.Entities;

namespace Tabatine.Infrastructure.Data.Configurations
{
    public class FormaPagamentoConfiguration : IEntityTypeConfiguration<FormaPagamento>
    {
        public void Configure(EntityTypeBuilder<FormaPagamento> builder)
        {
            builder.ToTable("formas_pagamento");
            builder.HasKey(f => f.Id);
            builder.HasIndex(f => f.Codigo).IsUnique();
            
            builder.Property(f => f.Codigo).HasMaxLength(20).IsRequired();
            builder.Property(f => f.Descricao).HasMaxLength(150).IsRequired();
            builder.Property(f => f.ListaParcelas).HasMaxLength(1000);
        }
    }
}
