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
    public class ClienteSyncService(
        IOmieClient omieClient, 
        AppDbContext dbContext, 
        ISyncStateRepository syncState, 
        ILogger<ClienteSyncService> logger) : ISyncService
    {
        public async Task SyncAllAsync(CancellationToken ct = default)
        {
            logger.LogInformation("Iniciando sincronização de Clientes...");

            var lastSyncDate = await syncState.GetLastSyncDateAsync("Clientes", ct);
            var syncStartTime = DateTime.UtcNow;

            var processedOmieIds = new HashSet<long>();
            int pagina = 1;
            bool temMais = true;

            while (temMais && !ct.IsCancellationRequested)
            {
                var response = await omieClient.ListarClientesAsync(pagina, filtrarDe: lastSyncDate, cancellationToken: ct);

                if (response == null || response.ClientesCadastro == null || response.ClientesCadastro.Count == 0) break;

                var omieIds = response.ClientesCadastro.Select(c => c.CodigoClienteOmie).ToList();
                var existingClientes = await dbContext.Clientes
                    .Where(c => omieIds.Contains(c.OmieId))
                    .ToDictionaryAsync(c => c.OmieId, ct);

                foreach (var omieCliente in response.ClientesCadastro)
                {
                    var omieId = omieCliente.CodigoClienteOmie;

                    if (!processedOmieIds.Add(omieId))
                    {
                        logger.LogWarning("Cliente OmieId {OmieId} duplicado na resposta da Omie. Pulando.", omieId);
                        continue;
                    }

                    existingClientes.TryGetValue(omieId, out var existing);
                    var omieLastAlt = OmieTimestampHelper.ParseOmieDateTime(omieCliente.DAlt, omieCliente.HAlt);

                    if (existing == null)
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

                        dbContext.Clientes.Add(novo);
                        existingClientes[omieId] = novo;
                    }
                    else
                    {
                        // Se o timestamp da Omie for igual ao que já temos, pula o update
                        if (existing.OmieUpdatedAt.HasValue && omieLastAlt.HasValue &&
                            existing.OmieUpdatedAt.Value == omieLastAlt.Value)
                        {
                            logger.LogDebug("Cliente OmieId {OmieId} já está atualizado. Pulando UPDATE.", omieCliente.CodigoClienteOmie);
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

                await dbContext.SaveChangesAsync(ct);
                logger.LogInformation("Página {Pagina} de {Total} de clientes sincronizada.", pagina, response.TotalDePaginas);

                temMais = pagina < response.TotalDePaginas;
                pagina++;
            }

            if (!ct.IsCancellationRequested)
            {
                await syncState.SetLastSyncDateAsync("Clientes", syncStartTime, ct);
            }

            logger.LogInformation("Sincronização de Clientes finalizada.");
        }
        public async Task SyncByIdAsync(long omieId, CancellationToken ct = default)
        {
            logger.LogInformation("Sincronizando Cliente/Fornecedor específico OmieId: {OmieId}", omieId);
            var omieCliente = await omieClient.ConsultarClienteAsync(omieId, ct);

            if (omieCliente != null)
            {
                var existing = await dbContext.Clientes.FirstOrDefaultAsync(c => c.OmieId == omieId, ct);
                var omieLastAlt = OmieTimestampHelper.ParseOmieDateTime(omieCliente.DAlt, omieCliente.HAlt);

                if (existing == null)
                {
                    dbContext.Clientes.Add(new Cliente
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
                        logger.LogDebug("Cliente OmieId {OmieId} já está atualizado. Pulando UPDATE.", omieId);
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

                await dbContext.SaveChangesAsync(ct);
                logger.LogInformation("Cliente/Fornecedor OmieId {OmieId} sincronizado individualmente com sucesso.", omieId);
            }
            else
            {
                logger.LogWarning("Cliente/Fornecedor OmieId {OmieId} não encontrado na Omie para consulta individual.", omieId);
            }
        }

        public Task CancelByIdAsync(long omieId, CancellationToken ct = default) => Task.CompletedTask;
    }
}
