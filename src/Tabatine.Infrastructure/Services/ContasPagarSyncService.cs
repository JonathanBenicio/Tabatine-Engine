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

            var processedOmieIds = new HashSet<long>();
            int pagina = 1;
            bool temMais = true;

            while (temMais && !ct.IsCancellationRequested)
            {
                // Nota: filtrar_por_vendedor removido — a Omie retorna 500 para este endpoint com esse filtro
                var response = await _omieClient.ListarContasPagarAsync(pagina, filtrarDe: lastSyncDate, cancellationToken: ct);

                if (response?.ContasPagar == null || response.ContasPagar.Count == 0) break;

                // Pré-carrega fornecedores (Clientes com EhFornecedor=true) e contas correntes
                var omieClienteIds = response.ContasPagar
                    .Where(t => t.CodigoClienteFornecedor > 0)
                    .Select(t => t.CodigoClienteFornecedor).Distinct().ToList();

                var clientes = await _dbContext.Clientes
                    .Where(c => omieClienteIds.Contains(c.OmieId))
                    .ToDictionaryAsync(c => c.OmieId, ct);

                var omieContaIds = response.ContasPagar
                    .Where(t => t.CodigoContaCorrente.HasValue)
                    .Select(t => t.CodigoContaCorrente!.Value).Distinct().ToList();

                var contasCorrente = await _dbContext.ContasCorrente
                    .Where(cc => omieContaIds.Contains(cc.OmieId))
                    .ToDictionaryAsync(cc => cc.OmieId, ct);

                var omieIds = response.ContasPagar.Select(t => t.CodigoLancamentoOmie).ToList();
                var existingTitulos = await _dbContext.TitulosPagar
                    .Where(t => omieIds.Contains(t.OmieId))
                    .ToDictionaryAsync(t => t.OmieId, ct);

                foreach (var omieItem in response.ContasPagar)
                {
                    var omieId = omieItem.CodigoLancamentoOmie;

                    if (!processedOmieIds.Add(omieId))
                    {
                        _logger.LogDebug("TituloPagar OmieId {OmieId} duplicado na resposta. Pulando.", omieId);
                        continue;
                    }

                    clientes.TryGetValue(omieItem.CodigoClienteFornecedor, out var cliente);
                    contasCorrente.TryGetValue(omieItem.CodigoContaCorrente ?? 0, out var contaCorrente);

                    existingTitulos.TryGetValue(omieId, out var existing);

                    if (existing == null)
                    {
                        var novo = MapToEntity(omieItem, cliente, contaCorrente);
                        _dbContext.TitulosPagar.Add(novo);
                        existingTitulos[omieId] = novo;
                    }
                    else
                    {
                        AtualizarEntidade(existing, omieItem, cliente, contaCorrente);
                    }
                }

                await _dbContext.SaveChangesAsync(ct);
                _logger.LogInformation("Página {Pagina} de {Total} de títulos a pagar sincronizada.", pagina, response.TotalDePaginas);

                temMais = pagina < response.TotalDePaginas;
                pagina++;
            }

            await _syncState.SetLastSyncDateAsync(SyncKey, syncStartTime, ct);
            _logger.LogInformation("Sincronização de Contas a Pagar finalizada. Total processado: {Count}", processedOmieIds.Count);
        }

        public async Task SyncByIdAsync(long omieId, CancellationToken ct = default)
        {
            _logger.LogInformation("Sincronizando TituloPagar específico OmieId: {OmieId}", omieId);
            var omieItem = await _omieClient.ConsultarContaPagarAsync(omieId, ct);

            if (omieItem != null)
            {
                // Busca dependências necessárias (Cliente/Fornecedor e Conta Corrente)
                var cliente = await _dbContext.Clientes.FirstOrDefaultAsync(c => c.OmieId == omieItem.CodigoClienteFornecedor, ct);
                var contaCorrente = omieItem.CodigoContaCorrente.HasValue 
                    ? await _dbContext.ContasCorrente.FirstOrDefaultAsync(cc => cc.OmieId == omieItem.CodigoContaCorrente.Value, ct) 
                    : null;

                var existing = await _dbContext.TitulosPagar.FirstOrDefaultAsync(t => t.OmieId == omieId, ct);

                if (existing == null)
                {
                    _dbContext.TitulosPagar.Add(MapToEntity(omieItem, cliente, contaCorrente));
                }
                else
                {
                    AtualizarEntidade(existing, omieItem, cliente, contaCorrente);
                }

                await _dbContext.SaveChangesAsync(ct);
                _logger.LogInformation("TituloPagar OmieId {OmieId} sincronizado com sucesso.", omieId);
            }
            else
            {
                _logger.LogWarning("TituloPagar OmieId {OmieId} não encontrado na Omie.", omieId);
            }
        }

        public async Task CancelByIdAsync(long omieId, CancellationToken ct = default)
        {
            _logger.LogInformation("Cancelando TituloPagar OmieId: {OmieId} localmente via soft-delete.", omieId);
            var existing = await _dbContext.TitulosPagar.FirstOrDefaultAsync(t => t.OmieId == omieId, ct);
            
            if (existing != null)
            {
                existing.StatusTitulo = "CANCELADO";
                existing.UpdatedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync(ct);
                _logger.LogInformation("TituloPagar OmieId {OmieId} marcado como CANCELADO.", omieId);
            }
        }

        private static TituloPagar MapToEntity(OmieContaPagar omie, Cliente? cliente, ContaCorrente? contaCorrente)
        {
            return new TituloPagar
            {
                Id = Guid.NewGuid(),
                OmieId = omie.CodigoLancamentoOmie,
                NumeroDocumento = omie.NumeroDocumento ?? string.Empty,
                NumeroPedido = omie.NumeroPedido,
                DataEmissao = ParseDataOptional(omie.DataEmissao) ?? DateTime.UtcNow,
                DataVencimento = ParseData(omie.DataVencimento),
                DataBaixa = ParseDataOptional(omie.DataBaixa),
                ValorDocumento = omie.ValorDocumento,
                ValorPago = omie.ValorPago,
                ValorSaldo = omie.ValorSaldo,
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
            existing.DataEmissao = ParseDataOptional(omie.DataEmissao) ?? existing.DataEmissao;
            existing.DataVencimento = ParseData(omie.DataVencimento);
            existing.DataBaixa = ParseDataOptional(omie.DataBaixa);
            existing.ValorDocumento = omie.ValorDocumento;
            existing.ValorPago = omie.ValorPago;
            existing.ValorSaldo = omie.ValorSaldo;
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

        private static DateTime? ParseDataOptional(string? data)
        {
            if (string.IsNullOrWhiteSpace(data)) return null;
            return ParseData(data);
        }
    }
}
