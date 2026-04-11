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
    public class ContasReceberSyncService(
        IOmieClient omieClient,
        AppDbContext dbContext,
        ISyncStateRepository syncState,
        ILogger<ContasReceberSyncService> logger) : ISyncService
    {
        private const string SyncKey = "LancamentosReceber";

        public async Task SyncAllAsync(CancellationToken ct = default)
        {
            logger.LogInformation("Iniciando sincronização de Contas a Receber...");

            var lastSyncDate = await syncState.GetLastSyncDateAsync(SyncKey, ct);
            var syncStartTime = DateTime.UtcNow;

            var processedOmieIds = new HashSet<long>();
            int pagina = 1;
            bool temMais = true;

            while (temMais && !ct.IsCancellationRequested)
            {
                var response = await omieClient.ListarContasReceberAsync(pagina, filtrarDe: lastSyncDate, cancellationToken: ct);

                if (response?.ContasReceber == null || response.ContasReceber.Count == 0) break;

                // Pré-carrega clientes e contas correntes referenciados nesta página
                var omieClienteIds = response.ContasReceber
                    .Where(t => t.CodigoClienteFornecedor > 0)
                    .Select(t => t.CodigoClienteFornecedor).Distinct().ToList();

                var omieVendedorIds = response.ContasReceber
                    .Where(t => t.CodigoVendedor.HasValue)
                    .Select(t => t.CodigoVendedor!.Value).Distinct().ToList();

                var omieContaIds = response.ContasReceber
                    .Where(t => t.CodigoContaCorrente.HasValue)
                    .Select(t => t.CodigoContaCorrente!.Value).Distinct().ToList();

                var clientes = await dbContext.Clientes
                    .Where(c => omieClienteIds.Contains(c.OmieId))
                    .ToDictionaryAsync(c => c.OmieId, ct);

                var vendedores = await dbContext.Vendedores
                    .Where(v => omieVendedorIds.Contains(v.OmieId))
                    .ToDictionaryAsync(v => v.OmieId, ct);

                var contasCorrente = await dbContext.ContasCorrente
                    .Where(cc => omieContaIds.Contains(cc.OmieId))
                    .ToDictionaryAsync(cc => cc.OmieId, ct);

                var omieIds = response.ContasReceber.Select(t => t.CodigoLancamentoOmie).ToList();
                var existingTitulos = await dbContext.TitulosReceber
                    .Where(t => omieIds.Contains(t.OmieId))
                    .ToDictionaryAsync(t => t.OmieId, ct);

                foreach (var omieItem in response.ContasReceber)
                {
                    var omieId = omieItem.CodigoLancamentoOmie;

                    if (!processedOmieIds.Add(omieId))
                    {
                        logger.LogDebug("TituloReceber OmieId {OmieId} duplicado na resposta. Pulando.", omieId);
                        continue;
                    }

                    clientes.TryGetValue(omieItem.CodigoClienteFornecedor, out var cliente);
                    vendedores.TryGetValue(omieItem.CodigoVendedor ?? 0, out var vendedor);
                    contasCorrente.TryGetValue(omieItem.CodigoContaCorrente ?? 0, out var contaCorrente);

                    existingTitulos.TryGetValue(omieId, out var existing);

                    if (existing == null)
                    {
                        var novo = MapToEntity(omieItem, cliente, vendedor, contaCorrente);
                        dbContext.TitulosReceber.Add(novo);
                        existingTitulos[omieId] = novo;
                    }
                    else
                    {
                        AtualizarEntidade(existing, omieItem, cliente, vendedor, contaCorrente);
                    }
                }

                await dbContext.SaveChangesAsync(ct);
                logger.LogInformation("Página {Pagina} de {Total} de Contas a Receber sincronizada.", pagina, response.TotalDePaginas);

                temMais = pagina < response.TotalDePaginas;
                pagina++;
            }

            await syncState.SetLastSyncDateAsync(SyncKey, syncStartTime, ct);
            logger.LogInformation("Sincronização de Contas a Receber finalizada. Total processado: {Count}", processedOmieIds.Count);
        }

        public async Task SyncByIdAsync(long omieId, CancellationToken ct = default)
        {
            logger.LogInformation("Sincronizando TituloReceber específico OmieId: {OmieId}", omieId);
            var omieItem = await omieClient.ConsultarContaReceberAsync(omieId, ct);

            if (omieItem != null)
            {
                // Busca dependências necessárias
                var cliente = await dbContext.Clientes.FirstOrDefaultAsync(c => c.OmieId == omieItem.CodigoClienteFornecedor, ct);
                var vendedor = omieItem.CodigoVendedor.HasValue 
                    ? await dbContext.Vendedores.FirstOrDefaultAsync(v => v.OmieId == omieItem.CodigoVendedor.Value, ct) 
                    : null;
                var contaCorrente = omieItem.CodigoContaCorrente.HasValue 
                    ? await dbContext.ContasCorrente.FirstOrDefaultAsync(cc => cc.OmieId == omieItem.CodigoContaCorrente.Value, ct) 
                    : null;

                var existing = await dbContext.TitulosReceber.FirstOrDefaultAsync(t => t.OmieId == omieId, ct);

                if (existing == null)
                {
                    dbContext.TitulosReceber.Add(MapToEntity(omieItem, cliente, vendedor, contaCorrente));
                }
                else
                {
                    AtualizarEntidade(existing, omieItem, cliente, vendedor, contaCorrente);
                }

                await dbContext.SaveChangesAsync(ct);
                logger.LogInformation("TituloReceber OmieId {OmieId} sincronizado com sucesso.", omieId);
            }
            else
            {
                logger.LogWarning("TituloReceber OmieId {OmieId} não encontrado na Omie.", omieId);
            }
        }

        public async Task CancelByIdAsync(long omieId, CancellationToken ct = default)
        {
            logger.LogInformation("Cancelando TituloReceber OmieId: {OmieId} localmente via soft-delete.", omieId);
            var existing = await dbContext.TitulosReceber.FirstOrDefaultAsync(t => t.OmieId == omieId, ct);
            
            if (existing != null)
            {
                existing.StatusTitulo = "CANCELADO";
                existing.UpdatedAt = DateTime.UtcNow;
                await dbContext.SaveChangesAsync(ct);
                logger.LogInformation("TituloReceber OmieId {OmieId} marcado como CANCELADO.", omieId);
            }
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
                DataBaixa = ParseDataOptional(omie.DataBaixa),
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
            existing.DataEmissao = ParseData(omie.DataEmissao);
            existing.DataVencimento = ParseData(omie.DataVencimento);
            existing.DataPrevisao = string.IsNullOrEmpty(omie.DataPrevisao) ? null : ParseData(omie.DataPrevisao);
            existing.DataBaixa = ParseDataOptional(omie.DataBaixa);
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

        private static DateTime? ParseDataOptional(string? data)
        {
            if (string.IsNullOrWhiteSpace(data)) return null;
            return ParseData(data);
        }
    }
}
