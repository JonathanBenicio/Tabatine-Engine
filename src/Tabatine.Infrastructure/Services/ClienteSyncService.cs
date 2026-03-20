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

            int pagina = 1;
            bool temMais = true;

            while (temMais && !ct.IsCancellationRequested)
            {
                var response = await _omieClient.ListarClientesAsync(pagina, filtrarDe: lastSyncDate, cancellationToken: ct);
                
                // Resposta nula = sem registros (Client-5113)
                if (response == null || response.ClientesCadastro == null || response.ClientesCadastro.Count == 0) break;

                var omieIds = response.ClientesCadastro.Select(c => c.CodigoClienteOmie).ToList();
                var existingClientes = await _dbContext.Clientes
                    .Where(c => omieIds.Contains(c.OmieId))
                    .ToDictionaryAsync(c => c.OmieId, ct);

                foreach (var omieCliente in response.ClientesCadastro)
                {
                    existingClientes.TryGetValue(omieCliente.CodigoClienteOmie, out var existing);
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
                        // Se o timestamp da Omie for igual ao que já temos, pula o update
                        if (existing.OmieUpdatedAt.HasValue && omieLastAlt.HasValue && 
                            existing.OmieUpdatedAt.Value == omieLastAlt.Value)
                        {
                            _logger.LogDebug("Cliente OmieId {OmieId} já está atualizado. Pulando UPDATE.", omieCliente.CodigoClienteOmie);
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
                }

                await _dbContext.SaveChangesAsync(ct);
                _logger.LogInformation("Página {Pagina} de {Total} sincronizada.", pagina, response.TotalDePaginas);

                temMais = pagina < response.TotalDePaginas;
                pagina++;
            }

            await _syncState.SetLastSyncDateAsync("Clientes", syncStartTime, ct);
            _logger.LogInformation("Sincronização de Clientes finalizada.");
        }
        public async Task SyncByIdAsync(long omieId, CancellationToken ct = default)
        {
            // Opcional: Implementar se necessário para webhooks de clientes
            await Task.CompletedTask;
        }
    }
}
