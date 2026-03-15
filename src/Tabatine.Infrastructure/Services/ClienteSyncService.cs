using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tabatine.Core.Entities;
using Tabatine.Core.Interfaces;
using Tabatine.Infrastructure.Data;
using Tabatine.Omie.Client;
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
        private readonly ILogger<ClienteSyncService> _logger;

        public ClienteSyncService(IOmieClient omieClient, AppDbContext dbContext, ILogger<ClienteSyncService> logger)
        {
            _omieClient = omieClient;
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task SyncAllAsync(CancellationToken ct = default)
        {
            _logger.LogInformation("Iniciando sincronização de Clientes...");

            int pagina = 1;
            bool temMais = true;

            while (temMais && !ct.IsCancellationRequested)
            {
                var response = await _omieClient.ListarClientesAsync(pagina, ct);
                
                if (response.ClientesCadastro.Count == 0) break;

                foreach (var omieCliente in response.ClientesCadastro)
                {
                    var existing = await _dbContext.Clientes
                        .FirstOrDefaultAsync(c => c.OmieId == omieCliente.CodigoClienteOmie, ct);

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
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        });
                    }
                    else
                    {
                        existing.RazaoSocial = omieCliente.RazaoSocial;
                        existing.NomeFantasia = omieCliente.NomeFantasia;
                        existing.CnpjCpf = omieCliente.CnpjCpf;
                        existing.Email = omieCliente.Email;
                        existing.UpdatedAt = DateTime.UtcNow;
                    }
                }

                await _dbContext.SaveChangesAsync(ct);
                _logger.LogInformation("Página {Pagina} de {Total} sincronizada.", pagina, response.TotalDePaginas);

                temMais = pagina < response.TotalDePaginas;
                pagina++;
            }

            _logger.LogInformation("Sincronização de Clientes finalizada.");
        }
    }
}
