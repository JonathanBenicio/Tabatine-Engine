using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tabatine.Core.Entities;
using Tabatine.Core.Interfaces;
using Tabatine.Infrastructure.Data;
using Tabatine.Omie.Client;
using Tabatine.Omie.Client.Models.Financeiro;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Tabatine.Infrastructure.Services
{
    public class ContasPagarSyncService : ISyncService
    {
        private readonly IOmieClient _omieClient;
        private readonly AppDbContext _dbContext;
        private readonly ISyncStateRepository _syncState;
        private readonly ILogger<ContasPagarSyncService> _logger;

        private const string SyncKey = "ContasPagar";

        public ContasPagarSyncService(
            IOmieClient omieClient,
            AppDbContext dbContext,
            ISyncStateRepository syncState,
            ILogger<ContasPagarSyncService> logger)
        {
            _omieClient = omieClient;
            _dbContext = dbContext;
            _syncState = syncState;
            _logger = logger;
        }

        public async Task SyncAllAsync(CancellationToken ct = default)
        {
            _logger.LogInformation("Iniciando sincronização de Contas a Pagar...");

            var lastSyncDate = await _syncState.GetLastSyncDateAsync(SyncKey, ct);
            var syncStartTime = DateTime.UtcNow;

            // Lookups locais para evitar N+1
            var existingLookup = await _dbContext.TitulosPagar.ToDictionaryAsync(t => t.OmieId, ct);
            var clientesLookup = await _dbContext.Clientes.ToDictionaryAsync(c => c.OmieId, ct);
            var contasCorrenteLookup = await _dbContext.ContasCorrente.ToDictionaryAsync(cc => cc.OmieId, ct);
            
            int count = 0;

            await foreach (var omieItem in _omieClient.StreamContasPagarAsync(filtrarDe: lastSyncDate, cancellationToken: ct))
            {
                var omieId = omieItem.CodigoLancamentoOmie;

                clientesLookup.TryGetValue(omieItem.CodigoClienteFornecedor, out var cliente);
                contasCorrenteLookup.TryGetValue(omieItem.CodigoContaCorrente ?? 0, out var contaCorrente);

                if (existingLookup.TryGetValue(omieId, out var existing))
                {
                    AtualizarEntidade(existing, omieItem, cliente, contaCorrente);
                }
                else
                {
                    var novo = MapToEntity(omieItem, cliente, contaCorrente);
                    _dbContext.TitulosPagar.Add(novo);
                    existingLookup[omieId] = novo;
                }

                if (++count % 500 == 0)
                {
                    await _dbContext.SaveChangesAsync(ct);
                    _logger.LogInformation("{Count} títulos a pagar processados...", count);
                }
            }

            await _dbContext.SaveChangesAsync(ct);
            await _syncState.SetLastSyncDateAsync(SyncKey, syncStartTime, ct);
            _logger.LogInformation("Sincronização de Contas a Pagar finalizada. Total: {Count}", count);
        }

        public async Task SyncByIdAsync(long omieId, CancellationToken ct = default)
        {
            // A API de Contas a Pagar da Omie não possui endpoint de consulta por ID direta.
            // O processo de webhooks deve acumular os IDs ou acionar um job assíncrono para evitar gargalos.
            _logger.LogWarning("SyncById solicitado para TituloPagar OmieId={OmieId}. A Omie não possui endpoint de consulta individual para Contas a Pagar. Requisição ignorada via webhook para evitar rate limiting.", omieId);
            await Task.CompletedTask;
        }

        private static TituloPagar MapToEntity(OmieContaPagar omie, Cliente? cliente, ContaCorrente? contaCorrente)
        {
            return new TituloPagar
            {
                Id = Guid.NewGuid(),
                OmieId = omie.CodigoLancamentoOmie,
                NumeroDocumento = omie.NumeroDocumento ?? string.Empty,
                NumeroPedido = omie.NumeroPedido,
                DataEmissao = DateTime.UtcNow, // A API de CP não retorna data de emissão diretamente
                DataVencimento = ParseData(omie.DataVencimento),
                ValorDocumento = omie.ValorDocumento,
                ValorPago = 0,
                ValorSaldo = omie.ValorDocumento,
                StatusTitulo = omie.StatusTitulo,
                CodigoCategoria = omie.CodigoCategoria,
                Observacao = omie.Observacao,
                ClienteId = cliente?.Id,
                ContaCorrenteId = contaCorrente?.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        private static void AtualizarEntidade(TituloPagar existing, OmieContaPagar omie, Cliente? cliente, ContaCorrente? contaCorrente)
        {
            existing.NumeroDocumento = omie.NumeroDocumento ?? string.Empty;
            existing.NumeroPedido = omie.NumeroPedido;
            existing.DataVencimento = ParseData(omie.DataVencimento);
            existing.ValorDocumento = omie.ValorDocumento;
            existing.StatusTitulo = omie.StatusTitulo;
            existing.CodigoCategoria = omie.CodigoCategoria;
            existing.Observacao = omie.Observacao;
            existing.ClienteId = cliente?.Id;
            existing.ContaCorrenteId = contaCorrente?.Id;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        // Omie retorna datas no formato dd/MM/yyyy
        private static DateTime ParseData(string data)
            => DateTime.ParseExact(data, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
    }
}
