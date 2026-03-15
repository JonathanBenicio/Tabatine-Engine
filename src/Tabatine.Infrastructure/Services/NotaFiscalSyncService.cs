using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tabatine.Core.Entities;
using Tabatine.Core.Interfaces;
using Tabatine.Infrastructure.Data;
using Tabatine.Omie.Client;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace Tabatine.Infrastructure.Services
{
    public class NotaFiscalSyncService : ISyncService
    {
        private readonly IOmieClient _omieClient;
        private readonly AppDbContext _dbContext;
        private readonly ILogger<NotaFiscalSyncService> _logger;

        public NotaFiscalSyncService(IOmieClient omieClient, AppDbContext dbContext, ILogger<NotaFiscalSyncService> logger)
        {
            _omieClient = omieClient;
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task SyncAllAsync(CancellationToken ct = default)
        {
            _logger.LogInformation("Iniciando sincronização de Notas Fiscais...");
            int pagina = 1;
            bool temMais = true;

            while (temMais && !ct.IsCancellationRequested)
            {
                var response = await _omieClient.ListarNotasFiscaisAsync(pagina, ct);
                if (response.NotasFiscais.Count == 0) break;

                foreach (var omieNf in response.NotasFiscais)
                {
                    var existing = await _dbContext.NotasFiscais
                        .FirstOrDefaultAsync(n => n.OmieId == omieNf.IdNf, ct);

                    var cliente = await _dbContext.Clientes
                        .FirstOrDefaultAsync(c => c.OmieId == omieNf.IdCliente, ct);

                    if (cliente == null) continue;

                    var pedido = omieNf.IdPedido.HasValue 
                        ? await _dbContext.PedidosVenda.FirstOrDefaultAsync(p => p.OmieId == omieNf.IdPedido.Value, ct)
                        : null;

                    DateTime dataEmissao;
                    DateTime.TryParseExact(omieNf.DataEmissao, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out dataEmissao);

                    if (existing == null)
                    {
                        _dbContext.NotasFiscais.Add(new NotaFiscal
                        {
                            Id = Guid.NewGuid(),
                            OmieId = omieNf.IdNf,
                            NumeroNf = omieNf.Numero,
                            ChaveAcesso = omieNf.ChaveNfe,
                            Status = omieNf.Status,
                            DataEmissao = dataEmissao,
                            ValorTotal = omieNf.ValorTotal,
                            ClienteId = cliente.Id,
                            PedidoVendaId = pedido?.Id,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        });
                    }
                    else
                    {
                        existing.Status = omieNf.Status;
                        existing.ChaveAcesso = omieNf.ChaveNfe;
                        existing.UpdatedAt = DateTime.UtcNow;
                    }
                }

                await _dbContext.SaveChangesAsync(ct);
                temMais = pagina < response.TotalDePaginas;
                pagina++;
            }
            _logger.LogInformation("Sincronização de Notas Fiscais finalizada.");
        }
    }
}
