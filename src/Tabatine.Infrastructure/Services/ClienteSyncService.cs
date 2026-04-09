using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tabatine.Core.Entities;
using Tabatine.Core.Interfaces;
using Tabatine.Infrastructure.Data;
using Tabatine.Omie.Client;
using Tabatine.Omie.Client.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Tabatine.Infrastructure.Services
{
    public class ClienteSyncService : ISyncService
    {
        private readonly IOmieClient _omieClient;
        private readonly AppDbContext _dbContext;
        private readonly ISyncStateRepository _syncState;
        private readonly ILogger<ClienteSyncService> _logger;

        public ClienteSyncService(IOmieClient omieClient, AppDbContext dbContext, ISyncStateRepository syncState, ILogger<ClienteSyncService> logger)
        {
            _omieClient = omieClient;
            _dbContext = dbContext;
            _syncState = syncState;
            _logger = logger;
        }

        public async Task SyncAllAsync(CancellationToken ct = default)
        {
            _logger.LogInformation("Iniciando sincronização de Clientes...");

            var lastSyncDate = await _syncState.GetLastSyncDateAsync("Clientes", ct);
            var syncStartTime = DateTime.UtcNow;

            // Carrega lookup local para evitar N+1
            var existingLookup = await _dbContext.Clientes.ToDictionaryAsync(c => c.OmieId, ct);
            int count = 0;

            await foreach (var omieCliente in _omieClient.StreamClientesAsync(filtrarDe: lastSyncDate, cancellationToken: ct))
            {
                var omieId = omieCliente.CodigoClienteOmie;
                var omieLastAlt = OmieTimestampHelper.ParseOmieDateTime(omieCliente.DAlt, omieCliente.HAlt);

                if (existingLookup.TryGetValue(omieId, out var existing))
                {
                    // Se o timestamp da Omie for igual ao que já temos, pula o update
                    if (existing.OmieUpdatedAt.HasValue && omieLastAlt.HasValue &&
                        existing.OmieUpdatedAt.Value == omieLastAlt.Value)
                    {
                        continue;
                    }

                    existing.RazaoSocial = omieCliente.RazaoSocial;
                    existing.NomeFantasia = omieCliente.NomeFantasia;
                    existing.CnpjCpf = omieCliente.CnpjCpf;
                    existing.Email = omieCliente.Email;
                    existing.Telefone = omieCliente.Telefone;
                    existing.Endereco = omieCliente.Endereco;
                    existing.EnderecoNumero = omieCliente.EnderecoNumero;
                    existing.EnderecoComplemento = omieCliente.Complemento;
                    existing.Bairro = omieCliente.Bairro;
                    existing.Cep = omieCliente.Cep;
                    existing.Estado = omieCliente.Estado;
                    existing.Cidade = omieCliente.Cidade;
                    existing.InscricaoEstadual = omieCliente.InscricaoEstadual;
                    existing.InscricaoMunicipal = omieCliente.InscricaoMunicipal;
                    existing.OptanteSimplesNacional = omieCliente.OptanteSimplesNacional == "S";
                    existing.UpdatedAt = DateTime.UtcNow;
                    existing.OmieUpdatedAt = omieLastAlt;
                }
                else
                {
                    var novo = new Cliente
                    {
                        Id = Guid.NewGuid(),
                        OmieId = omieCliente.CodigoClienteOmie,
                        RazaoSocial = omieCliente.RazaoSocial,
                        NomeFantasia = omieCliente.NomeFantasia,
                        CnpjCpf = omieCliente.CnpjCpf,
                        Email = omieCliente.Email,
                        Telefone = omieCliente.Telefone,
                        Endereco = omieCliente.Endereco,
                        EnderecoNumero = omieCliente.EnderecoNumero,
                        EnderecoComplemento = omieCliente.Complemento,
                        Bairro = omieCliente.Bairro,
                        Cep = omieCliente.Cep,
                        Estado = omieCliente.Estado,
                        Cidade = omieCliente.Cidade,
                        InscricaoEstadual = omieCliente.InscricaoEstadual,
                        InscricaoMunicipal = omieCliente.InscricaoMunicipal,
                        OptanteSimplesNacional = omieCliente.OptanteSimplesNacional == "S",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        OmieUpdatedAt = omieLastAlt
                    };

                    _dbContext.Clientes.Add(novo);
                    existingLookup[omieId] = novo;
                }

                if (++count % 500 == 0)
                {
                    await _dbContext.SaveChangesAsync(ct);
                    _logger.LogInformation("{Count} clientes processados...", count);
                }
            }

            await _dbContext.SaveChangesAsync(ct);
            await _syncState.SetLastSyncDateAsync("Clientes", syncStartTime, ct);
            _logger.LogInformation("Sincronização de Clientes finalizada. Total: {Count}", count);
        }
        public async Task SyncByIdAsync(long omieId, CancellationToken ct = default)
        {
            _logger.LogInformation("Sincronizando Cliente/Fornecedor específico OmieId: {OmieId}", omieId);
            var omieCliente = await _omieClient.ConsultarClienteAsync(omieId, ct);

            if (omieCliente != null)
            {
                var existing = await _dbContext.Clientes.FirstOrDefaultAsync(c => c.OmieId == omieId, ct);
                var omieLastAlt = OmieTimestampHelper.ParseOmieDateTime(omieCliente.DAlt, omieCliente.HAlt);

                if (existing == null)
                {
                    _dbContext.Clientes.Add(new Cliente
                    {
                        Id = Guid.NewGuid(),
                        OmieId = omieCliente.CodigoClienteOmie,
                        RazaoSocial = omieCliente.RazaoSocial,
                        NomeFantasia = omieCliente.NomeFantasia,
                        CnpjCpf = omieCliente.CnpjCpf,
                        Email = omieCliente.Email,
                        Telefone = omieCliente.Telefone,
                        Endereco = omieCliente.Endereco,
                        EnderecoNumero = omieCliente.EnderecoNumero,
                        EnderecoComplemento = omieCliente.Complemento,
                        Bairro = omieCliente.Bairro,
                        Cep = omieCliente.Cep,
                        Estado = omieCliente.Estado,
                        Cidade = omieCliente.Cidade,
                        InscricaoEstadual = omieCliente.InscricaoEstadual,
                        InscricaoMunicipal = omieCliente.InscricaoMunicipal,
                        OptanteSimplesNacional = omieCliente.OptanteSimplesNacional == "S",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        OmieUpdatedAt = omieLastAlt
                    });
                }
                else
                {
                    if (existing.OmieUpdatedAt.HasValue && omieLastAlt.HasValue &&
                        existing.OmieUpdatedAt.Value == omieLastAlt.Value)
                    {
                        _logger.LogDebug("Cliente OmieId {OmieId} já está atualizado. Pulando UPDATE.", omieId);
                        return;
                    }

                    existing.RazaoSocial = omieCliente.RazaoSocial;
                    existing.NomeFantasia = omieCliente.NomeFantasia;
                    existing.CnpjCpf = omieCliente.CnpjCpf;
                    existing.Email = omieCliente.Email;
                    existing.Telefone = omieCliente.Telefone;
                    existing.Endereco = omieCliente.Endereco;
                    existing.EnderecoNumero = omieCliente.EnderecoNumero;
                    existing.EnderecoComplemento = omieCliente.Complemento;
                    existing.Bairro = omieCliente.Bairro;
                    existing.Cep = omieCliente.Cep;
                    existing.Estado = omieCliente.Estado;
                    existing.Cidade = omieCliente.Cidade;
                    existing.InscricaoEstadual = omieCliente.InscricaoEstadual;
                    existing.InscricaoMunicipal = omieCliente.InscricaoMunicipal;
                    existing.OptanteSimplesNacional = omieCliente.OptanteSimplesNacional == "S";
                    existing.UpdatedAt = DateTime.UtcNow;
                    existing.OmieUpdatedAt = omieLastAlt;
                }

                await _dbContext.SaveChangesAsync(ct);
                _logger.LogInformation("Cliente/Fornecedor OmieId {OmieId} sincronizado individualmente com sucesso.", omieId);
            }
            else
            {
                _logger.LogWarning("Cliente/Fornecedor OmieId {OmieId} não encontrado na Omie para consulta individual.", omieId);
            }
        }
    }
}
