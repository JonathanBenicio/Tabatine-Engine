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
        public DbSet<ItemNotaFiscal> ItensNotaFiscal => Set<ItemNotaFiscal>();
        public DbSet<NotaFiscalTitulo> NotaFiscalTitulos => Set<NotaFiscalTitulo>();
        public DbSet<Vendedor> Vendedores => Set<Vendedor>();
        public DbSet<ContaCorrente> ContasCorrente => Set<ContaCorrente>();
        public DbSet<PedidoParcela> PedidoParcelas => Set<PedidoParcela>();
        public DbSet<IntegrationSyncState> IntegrationSyncStates => Set<IntegrationSyncState>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }
    }
}
