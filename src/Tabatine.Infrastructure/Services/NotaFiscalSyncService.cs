using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tabatine.Core.Entities;
using Tabatine.Core.Interfaces;
using Tabatine.Infrastructure.Data;
using Tabatine.Omie.Client;

namespace Tabatine.Infrastructure.Services
{
  public class NotaFiscalSyncService(IOmieClient omieClient, AppDbContext dbContext, ISyncStateRepository syncState, ILogger<NotaFiscalSyncService> logger) : ISyncService
  {
    private readonly IOmieClient _omieClient = omieClient;
    private readonly AppDbContext _dbContext = dbContext;
    private readonly ISyncStateRepository _syncState = syncState;
    private readonly ILogger<NotaFiscalSyncService> _logger = logger;

    public async Task SyncAllAsync(CancellationToken ct = default)
    {
      _logger.LogInformation("Iniciando sincronização de Notas Fiscais...");

      DateTime? lastSyncDate = await _syncState.GetLastSyncDateAsync("NotasFiscais", ct);
      DateTime syncStartTime = DateTime.UtcNow;

      // Rastreia OmieIds já processados neste ciclo para evitar duplicatas
      HashSet<long> processedOmieIds = new HashSet<long>();

      int pagina = 1;
      bool temMais = true;

      while (temMais && !ct.IsCancellationRequested)
      {
        Omie.Client.Models.NotasFiscais.ListarNotasFiscaisResponse response = await _omieClient.ListarNotasFiscaisAsync(pagina, filtrarDe: lastSyncDate, cancellationToken: ct);

        // Resposta nula = sem registros (Client-5113)
        if (response == null || response.NotasFiscais == null || response.NotasFiscais.Count == 0)
        {
          break;
        }

        List<long> omieNfIds = response.NotasFiscais.Select(n => n.Compl.IdNf).ToList();
        List<long> omieClienteIds = response.NotasFiscais.Select(n => n.Destinatario.CodigoCliente).Distinct().ToList();
        List<long> omiePedidoIds = response.NotasFiscais.Where(n => n.Compl.IdPedido.HasValue).Select(n => n.Compl.IdPedido!.Value).Distinct().ToList();

        // Coleta todos os nCodVendedor distintos dos títulos desta página
        List<long> omieVendedorIds = response.NotasFiscais
            .Where(n => n.Titulos != null)
            .SelectMany(n => n.Titulos!)
            .Select(t => t.CodigoVendedor)
            .Where(id => id > 0)
            .Distinct().ToList();

        Dictionary<long, NotaFiscal> existingNfs = await _dbContext.NotasFiscais
                    .Include(n => n.Itens)
                    .Include(n => n.Titulos)
                    .Where(n => omieNfIds.Contains(n.OmieId))
                    .ToDictionaryAsync(n => n.OmieId, ct);

        Dictionary<long, Cliente> clientes = await _dbContext.Clientes
                    .Where(c => omieClienteIds.Contains(c.OmieId))
                    .ToDictionaryAsync(c => c.OmieId, ct);

        var allOmieProdIds = response.NotasFiscais.SelectMany(n => n.Det.Select(d => d.Prod.Codigo.ToString())).Distinct().ToList();
        var produtos = await _dbContext.Produtos
                    .Where(p => allOmieProdIds.Contains(p.CodigoProduto))
                    .ToDictionaryAsync(p => p.CodigoProduto, ct);

        Dictionary<long, PedidoVenda> pedidos = await _dbContext.PedidosVenda
                    .Where(p => omiePedidoIds.Contains(p.OmieId))
                    .ToDictionaryAsync(p => p.OmieId, ct);

        // Busca em lote: Vendedores referenciados nos títulos
        Dictionary<long, Vendedor> vendedores = await _dbContext.Vendedores
                    .Where(v => omieVendedorIds.Contains(v.OmieId))
                    .ToDictionaryAsync(v => v.OmieId, ct);
        
        _logger.LogInformation("Página {Pagina}: Processando {NfCount} notas fiscais, {ProdIdCount} IDs de produtos, {ProdCount} produtos no banco.", pagina, response.NotasFiscais.Count, allOmieProdIds.Count, produtos.Count);

        foreach (Omie.Client.Models.NotasFiscais.OmieNotaFiscal omieNf in response.NotasFiscais)
        {
          long omieId = omieNf.Compl.IdNf;

          // Pula se já processamos este OmieId neste ciclo
          if (!processedOmieIds.Add(omieId))
          {
            _logger.LogDebug("NF OmieId {OmieId} duplicada na resposta. Pulando.", omieId);
            continue;
          }

          if (!clientes.TryGetValue(omieNf.Destinatario.CodigoCliente, out Cliente? cliente))
          {
            continue;
          }

          _ = pedidos.TryGetValue(omieNf.Compl.IdPedido ?? 0, out PedidoVenda? pedido);

          _ = existingNfs.TryGetValue(omieId, out NotaFiscal? existing);

          DateTime dataEmissao = DateTime.UtcNow;

          if (DateTime.TryParseExact(omieNf.Ide.DataEmissao, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dataParsed))
          {
            dataEmissao = DateTime.SpecifyKind(dataParsed, DateTimeKind.Utc);
          }

          string status = omieNf.Ide.Situacao switch
          {
            "101" => "CANCELADA",
            "110" => "DENEGADA",
            "301" => "DENEGADA", // Denegada (Uso Indevido)
            _ => omieNf.Ide.Denegada == "S" ? "DENEGADA" : "AUTORIZADA"
          };

          TimeSpan? horaEmissao = null;
          if (TimeSpan.TryParse(omieNf.Ide.HoraEmissao, out TimeSpan horaParsed))
          {
            horaEmissao = horaParsed;
          }

          if (existing == null)
          {
            existing = new NotaFiscal
            {
              Id = Guid.NewGuid(),
              OmieId = omieId,
              NumeroNf = omieNf.Ide.Numero,
              ChaveAcesso = omieNf.Compl.ChaveNfe,
              Status = status,
              CodigoStatus = int.TryParse(omieNf.Ide.Situacao, out int csCriacao) ? csCriacao : 0,
              DataEmissao = dataEmissao,
              HoraEmissao = horaEmissao,
              ValorTotal = omieNf.Total.IcmsTot.ValorNota,
              ValorIss = omieNf.Total.IssqnTot?.ValorIss ?? 0,
              ValorIr = omieNf.Total.RetTrib?.ValorIrrf ?? 0,
              ValorCsll = omieNf.Total.RetTrib?.ValorCsll ?? 0,
              ValorPisRetido = omieNf.Total.RetTrib?.ValorPis ?? 0,
              ValorCofinsRetido = omieNf.Total.RetTrib?.ValorCofins ?? 0,
              Denegada = omieNf.Ide.Denegada == "S",
              ClienteId = cliente.Id,
              PedidoVendaId = pedido?.Id,
              VendedorId = pedido?.VendedorId,
              ContaCorrenteId = pedido?.ContaCorrenteId,
              CreatedAt = DateTime.UtcNow,
              UpdatedAt = DateTime.UtcNow,
              Itens = new List<ItemNotaFiscal>(),
              Titulos = new List<NotaFiscalTitulo>()
            };
            _dbContext.NotasFiscais.Add(existing);
          }
          else
          {
            existing.Status = status;
            existing.CodigoStatus = int.TryParse(omieNf.Ide.Situacao, out int csAtualizacao) ? csAtualizacao : 0;
            existing.ChaveAcesso = omieNf.Compl.ChaveNfe;
            existing.HoraEmissao = horaEmissao;
            existing.ValorIss = omieNf.Total.IssqnTot?.ValorIss ?? 0;
            existing.ValorIr = omieNf.Total.RetTrib?.ValorIrrf ?? 0;
            existing.ValorCsll = omieNf.Total.RetTrib?.ValorCsll ?? 0;
            existing.ValorPisRetido = omieNf.Total.RetTrib?.ValorPis ?? 0;
            existing.ValorCofinsRetido = omieNf.Total.RetTrib?.ValorCofins ?? 0;
            existing.Denegada = omieNf.Ide.Denegada == "S";
            existing.VendedorId = pedido?.VendedorId;
            existing.ContaCorrenteId = pedido?.ContaCorrenteId;
            existing.UpdatedAt = DateTime.UtcNow;

            foreach (var item in existing.Itens.ToList()) _dbContext.ItensNotaFiscal.Remove(item);
            existing.Itens.Clear();
            foreach (var titulo in existing.Titulos.ToList()) _dbContext.NotaFiscalTitulos.Remove(titulo);
            existing.Titulos.Clear();
          }

          // Adiciona Itens
          int itensMapeados = 0;
          foreach (var det in omieNf.Det)
          {
              if (produtos.TryGetValue(det.Prod.Codigo, out var produto))
              {
                  existing.Itens.Add(new ItemNotaFiscal
                  {
                      Id = Guid.NewGuid(),
                      NotaFiscalId = existing.Id,
                      ProdutoId = produto.Id,
                      Quantidade = det.Prod.Quantidade,
                      ValorUnitario = det.Prod.ValorUnitario,
                      ValorTotal = det.Prod.ValorTotal
                  });
                  itensMapeados++;
              }
          }

          // Adiciona Títulos
          int titulosMapeados = 0;
          if (omieNf.Titulos != null)
          {
              foreach (var tit in omieNf.Titulos)
              {
                  if (DateTime.TryParseExact(tit.DataVencimento, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dtVenc))
                  {
                      // Vendedor do título vem diretamente do JSON; Conta Corrente vem do Pedido vinculado
                      vendedores.TryGetValue(tit.CodigoVendedor, out var titVendedor);

                      existing.Titulos.Add(new NotaFiscalTitulo
                      {
                          Id = Guid.NewGuid(),
                          NotaFiscalId = existing.Id,
                          OmieIdTitulo = tit.OmieIdTitulo,
                          NumeroParcela = tit.Parcela,
                          Valor = tit.Valor,
                          DataVencimento = DateTime.SpecifyKind(dtVenc, DateTimeKind.Utc),
                          ContaCorrenteId = pedido?.ContaCorrenteId,
                          VendedorId = titVendedor?.Id
                      });
                      titulosMapeados++;
                  }
              }
          }
          _logger.LogInformation("Nota Fiscal {NumeroNf}: {Itens} itens e {Titulos} títulos mapeados.", omieNf.Ide.Numero, itensMapeados, titulosMapeados);
        }

        try
        {
          await _dbContext.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
          _logger.LogWarning("Concorrência detectada ao salvar página {Pagina} de notas fiscais. Limpando rastreador e ignorando conflito.", pagina);
          foreach (var entry in _dbContext.ChangeTracker.Entries().ToList()) entry.State = EntityState.Detached;
        }

        _logger.LogInformation("Página {Pagina} de {Total} de notas fiscais sincronizada.", pagina, response.TotalDePaginas);
        temMais = pagina < response.TotalDePaginas;
        pagina++;
      }

      await _syncState.SetLastSyncDateAsync("NotasFiscais", syncStartTime, ct);
      _logger.LogInformation("Sincronização de Notas Fiscais finalizada.");
    }
  }
}
