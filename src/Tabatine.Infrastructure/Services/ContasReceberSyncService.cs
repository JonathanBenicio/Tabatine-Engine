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
    public class ContasReceberSyncService : ISyncService
    {
        private readonly IOmieClient _omieClient;
        private readonly AppDbContext _dbContext;
        private readonly ISyncStateRepository _syncState;
        private readonly ILogger<ContasReceberSyncService> _logger;

        private const string SyncKey = "ContasReceber";

        public ContasReceberSyncService(
            IOmieClient omieClient,
            AppDbContext dbContext,
            ISyncStateRepository syncState,
            ILogger<ContasReceberSyncService> logger)
        {
            _omieClient = omieClient;
            _dbContext = dbContext;
            _syncState = syncState;
            _logger = logger;
        }

        public async Task SyncAllAsync(CancellationToken ct = default)
        {
            _logger.LogInformation("Iniciando sincronização de Contas a Receber...");

            var lastSyncDate = await _syncState.GetLastSyncDateAsync(SyncKey, ct);
            var syncStartTime = DateTime.UtcNow;

            // Lookups locais para evitar N+1
            var existingLookup = await _dbContext.TitulosReceber.ToDictionaryAsync(t => t.OmieId, ct);
            var clientesLookup = await _dbContext.Clientes.ToDictionaryAsync(c => c.OmieId, ct);
            var vendedoresLookup = await _dbContext.Vendedores.ToDictionaryAsync(v => v.OmieId, ct);
            var contasCorrenteLookup = await _dbContext.ContasCorrente.ToDictionaryAsync(cc => cc.OmieId, ct);
            
            int count = 0;

            await foreach (var omieItem in _omieClient.StreamContasReceberAsync(filtrarDe: lastSyncDate, cancellationToken: ct))
            {
                var omieId = omieItem.CodigoLancamentoOmie;

                clientesLookup.TryGetValue(omieItem.CodigoClienteFornecedor, out var cliente);
                vendedoresLookup.TryGetValue(omieItem.CodigoVendedor ?? 0, out var vendedor);
                contasCorrenteLookup.TryGetValue(omieItem.CodigoContaCorrente ?? 0, out var contaCorrente);

                if (existingLookup.TryGetValue(omieId, out var existing))
                {
                    AtualizarEntidade(existing, omieItem, cliente, vendedor, contaCorrente);
                }
                else
                {
                    var novo = MapToEntity(omieItem, cliente, vendedor, contaCorrente);
                    _dbContext.TitulosReceber.Add(novo);
                    existingLookup[omieId] = novo;
                }

                if (++count % 500 == 0)
                {
                    await _dbContext.SaveChangesAsync(ct);
                    _logger.LogInformation("{Count} títulos a receber processados...", count);
                }
            }

            await _dbContext.SaveChangesAsync(ct);
            await _syncState.SetLastSyncDateAsync(SyncKey, syncStartTime, ct);
            _logger.LogInformation("Sincronização de Contas a Receber finalizada. Total: {Count}", count);
        }

        public async Task SyncByIdAsync(long omieId, CancellationToken ct = default)
        {
            // A API de Contas a Receber da Omie não possui endpoint de consulta por ID direta.
            // O processo de webhooks deve acumular os IDs ou acionar um job assíncrono para evitar gargalos.
            _logger.LogWarning("SyncById solicitado para TituloReceber OmieId={OmieId}. A Omie não possui endpoint de consulta individual para Contas a Receber. Requisição ignorada via webhook para evitar rate limiting.", omieId);
            await Task.CompletedTask;
        }

        private static TituloReceber MapToEntity(
            OmieContaReceber omie,
            Cliente? cliente,
            Vendedor? vendedor,
            ContaCorrente? contaCorrente)
        {
            return new TituloReceber
            {
                Id = Guid.NewGuid(),
                OmieId = omie.CodigoLancamentoOmie,
                NumeroDocumento = omie.NumeroDocumento ?? string.Empty,
                NumeroParcela = omie.NumeroParcela,
                NumeroPedido = omie.NumeroPedido,
                DataEmissao = ParseData(omie.DataEmissao),
                DataVencimento = ParseData(omie.DataVencimento),
                DataPrevisao = string.IsNullOrEmpty(omie.DataPrevisao) ? null : ParseData(omie.DataPrevisao),
                ValorDocumento = omie.ValorDocumento,
                ValorRecebido = omie.ValorRecebido,
                ValorSaldo = omie.ValorSaldo,
                StatusTitulo = omie.StatusTitulo,
                CodigoCategoria = omie.CodigoCategoria,
                Observacao = omie.Observacao,
                ClienteId = cliente?.Id,
                VendedorId = vendedor?.Id,
                ContaCorrenteId = contaCorrente?.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        private static void AtualizarEntidade(
            TituloReceber existing,
            OmieContaReceber omie,
            Cliente? cliente,
            Vendedor? vendedor,
            ContaCorrente? contaCorrente)
        {
            existing.NumeroDocumento = omie.NumeroDocumento ?? string.Empty;
            existing.NumeroParcela = omie.NumeroParcela;
            existing.NumeroPedido = omie.NumeroPedido;
            existing.DataVencimento = ParseData(omie.DataVencimento);
            existing.DataPrevisao = string.IsNullOrEmpty(omie.DataPrevisao) ? null : ParseData(omie.DataPrevisao);
            existing.ValorDocumento = omie.ValorDocumento;
            existing.ValorRecebido = omie.ValorRecebido;
            existing.ValorSaldo = omie.ValorSaldo;
            existing.StatusTitulo = omie.StatusTitulo;
            existing.CodigoCategoria = omie.CodigoCategoria;
            existing.Observacao = omie.Observacao;
            existing.ClienteId = cliente?.Id;
            existing.VendedorId = vendedor?.Id;
            existing.ContaCorrenteId = contaCorrente?.Id;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        // Omie retorna datas no formato dd/MM/yyyy
        private static DateTime ParseData(string data)
            => DateTime.ParseExact(data, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
    }
}
