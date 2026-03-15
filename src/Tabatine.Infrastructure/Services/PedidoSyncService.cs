using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tabatine.Core.Entities;
using Tabatine.Core.Interfaces;
using Tabatine.Infrastructure.Data;
using Tabatine.Omie.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Tabatine.Infrastructure.Services
{
    public class PedidoSyncService : ISyncService
    {
        private readonly IOmieClient _omieClient;
        private readonly AppDbContext _dbContext;
        private readonly ILogger<PedidoSyncService> _logger;

        public PedidoSyncService(IOmieClient omieClient, AppDbContext dbContext, ILogger<PedidoSyncService> logger)
        {
            _omieClient = omieClient;
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task SyncAllAsync(CancellationToken ct = default)
        {
            _logger.LogInformation("Iniciando sincronização de Pedidos de Venda...");
            int pagina = 1;
            bool temMais = true;

            while (temMais && !ct.IsCancellationRequested)
            {
                var response = await _omieClient.ListarPedidosAsync(pagina, ct);
                if (response.PedidosVenda.Count == 0) break;

                foreach (var omiePedido in response.PedidosVenda)
                {
                    var existing = await _dbContext.PedidosVenda
                        .Include(p => p.Itens)
                        .FirstOrDefaultAsync(p => p.OmieId == omiePedido.Cabecalho.CodigoPedidoOmie, ct);

                    var cliente = await _dbContext.Clientes
                        .FirstOrDefaultAsync(c => c.OmieId == omiePedido.Cabecalho.CodigoCliente, ct);

                    if (cliente == null)
                    {
                        _logger.LogWarning("Cliente {Id} não encontrado. Pulando pedido {Ped}.", omiePedido.Cabecalho.CodigoCliente, omiePedido.Cabecalho.NumeroPedido);
                        continue;
                    }

                    if (existing == null)
                    {
                        var novoPedido = new PedidoVenda
                        {
                            Id = Guid.NewGuid(),
                            OmieId = omiePedido.Cabecalho.CodigoPedidoOmie,
                            NumeroPedido = omiePedido.Cabecalho.NumeroPedido,
                            Etapa = omiePedido.Cabecalho.Etapa,
                            ValorTotal = omiePedido.Cabecalho.ValorTotal,
                            ClienteId = cliente.Id,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };

                        foreach (var item in omiePedido.Detalhe)
                        {
                            var produto = await _dbContext.Produtos
                                .FirstOrDefaultAsync(p => p.OmieId == item.Produto.CodigoProduto, ct);
                            
                            if (produto == null) continue;

                            novoPedido.Itens.Add(new ItemPedido
                            {
                                Id = Guid.NewGuid(),
                                PedidoVendaId = novoPedido.Id,
                                ProdutoId = produto.Id,
                                Quantidade = (int)item.Produto.Quantidade,
                                ValorUnitario = item.Produto.ValorUnitario,
                                ValorTotal = item.Produto.Quantidade * item.Produto.ValorUnitario,
                                CreatedAt = DateTime.UtcNow
                            });
                        }

                        _dbContext.PedidosVenda.Add(novoPedido);
                    }
                    else
                    {
                        existing.Etapa = omiePedido.Cabecalho.Etapa;
                        existing.ValorTotal = omiePedido.Cabecalho.ValorTotal;
                        existing.UpdatedAt = DateTime.UtcNow;
                        // Simplificação: Itens de pedido geralmente não mudam drasticamente após criados,
                        // mas em um sistema real, você deveria sincronizar itens também.
                    }
                }

                await _dbContext.SaveChangesAsync(ct);
                temMais = pagina < response.TotalDePaginas;
                pagina++;
            }
            _logger.LogInformation("Sincronização de Pedidos finalizada.");
        }
    }
}
