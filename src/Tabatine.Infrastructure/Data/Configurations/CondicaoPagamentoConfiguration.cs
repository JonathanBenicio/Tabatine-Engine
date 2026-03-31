using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tabatine.Core.Entities;

namespace Tabatine.Infrastructure.Data.Configurations
{
    public class CondicaoPagamentoConfiguration : IEntityTypeConfiguration<CondicaoPagamento>
    {
        public void Configure(EntityTypeBuilder<CondicaoPagamento> builder)
        {
            builder.ToTable("condicoes_pagamento");
            builder.HasKey(c => c.Id);
            builder.HasIndex(c => c.Codigo).IsUnique();

            builder.Property(c => c.Codigo).HasMaxLength(20).IsRequired();
            builder.Property(c => c.Descricao).HasMaxLength(150).IsRequired();

            // Relacionamento com PedidoVenda
            builder.HasMany(c => c.PedidosVenda)
                   .WithOne(p => p.CondicaoPagamento)
                   .HasForeignKey(p => p.CondicaoPagamentoId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
