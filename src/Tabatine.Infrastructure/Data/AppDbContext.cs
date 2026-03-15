using Microsoft.EntityFrameworkCore;
using Tabatine.Core.Entities;

namespace Tabatine.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Cliente> Clientes => Set<Cliente>();
        public DbSet<Produto> Produtos => Set<Produto>();
        public DbSet<PedidoVenda> PedidosVenda => Set<PedidoVenda>();
        public DbSet<ItemPedido> ItensPedido => Set<ItemPedido>();
        public DbSet<NotaFiscal> NotasFiscais => Set<NotaFiscal>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }
    }
}
