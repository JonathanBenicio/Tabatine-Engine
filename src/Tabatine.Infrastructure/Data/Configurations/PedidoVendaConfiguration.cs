using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tabatine.Core.Entities;

namespace Tabatine.Infrastructure.Data.Configurations
{
    public class PedidoVendaConfiguration : IEntityTypeConfiguration<PedidoVenda>
    {
        public void Configure(EntityTypeBuilder<PedidoVenda> builder)
        {
            builder.ToTable("PedidosVenda");
            builder.HasKey(p => p.Id);
            builder.HasIndex(p => p.OmieId).IsUnique();
            
            builder.HasOne(p => p.Cliente)
                   .WithMany(c => c.Pedidos)
                   .HasForeignKey(p => p.ClienteId)
                   .OnDelete(DeleteBehavior.Restrict);
                   
            builder.HasOne(p => p.Vendedor)
                   .WithMany(v => v.PedidosVenda)
                   .HasForeignKey(p => p.VendedorId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(p => p.ContaCorrente)
                   .WithMany(c => c.PedidosVenda)
                   .HasForeignKey(p => p.ContaCorrenteId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(p => p.EtapaFaturamento)
                   .WithMany()
                   .HasForeignKey(p => p.EtapaFaturamentoId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(p => p.FormaPagamento)
                   .WithMany()
                   .HasForeignKey(p => p.FormaPagamentoId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(p => p.CondicaoPagamento)
                   .WithMany()
                   .HasForeignKey(p => p.CondicaoPagamentoId)
                   .OnDelete(DeleteBehavior.Restrict);
                   
            builder.Property(p => p.ValorTotal)
                   .HasColumnType("numeric(18,2)");

            builder.Property(p => p.ValorFrete)
                   .HasColumnType("numeric(18,2)");

            builder.Property(p => p.PesoBruto)
                   .HasColumnType("numeric(18,3)");

            builder.Property(p => p.PesoLiquido)
                   .HasColumnType("numeric(18,3)");

            builder.Property(p => p.ValorIcms)
                   .HasColumnType("numeric(18,2)");

            builder.Property(p => p.ValorIpi)
                   .HasColumnType("numeric(18,2)");

            builder.Property(p => p.ValorPis)
                   .HasColumnType("numeric(18,2)");

            builder.Property(p => p.ValorCofins)
                   .HasColumnType("numeric(18,2)");

            builder.Property(p => p.BaseCalculoIcms)
                   .HasColumnType("numeric(18,2)");

            builder.Property(p => p.ValorMercadorias)
                   .HasColumnType("numeric(18,2)");

            builder.Property(p => p.ComissaoVendedor)
                   .HasColumnType("numeric(10,2)");

            builder.Property(p => p.ValorDesconto)
                   .HasColumnType("numeric(18,2)");

            builder.Property(p => p.ValorIbs)
                   .HasColumnType("numeric(18,2)");

            builder.Property(p => p.ValorCbs)
                   .HasColumnType("numeric(18,2)");

            builder.Property(p => p.ValorSeguro)
                   .HasColumnType("numeric(18,2)");

            builder.Property(p => p.ValorOutrasDespesas)
                   .HasColumnType("numeric(18,2)");
        }
    }
}
