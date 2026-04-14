using System.Net.Http.Json;
using Tabatine.Core.Entities;
using Tabatine.Omie.Client.Models.Estoque;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tabatine.Infrastructure.Data;
using Xunit;
using NSubstitute;

namespace Tabatine.Worker.IntegrationTests;

public class EstoqueWebhookIntegrationTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task Deve_Processar_Webhook_LocalEstoque_E_Sincronizar_Locais()
    {
        // Arrange
        var localOmieId = 555666L;
        var localDto = new LocalEstoqueDto
        {
            CodigoLocalEstoque = localOmieId,
            Codigo = "LOC-TESTE",
            Descricao = "Local de Teste Webhook",
            Padrao = "N",
            Inativo = "N"
        };

        // Mockagem: SyncLocalByIdAsync chama SyncLocaisAsync que usa ListarLocaisEstoqueAsync
        Factory.OmieClientMock.ListarLocaisEstoqueAsync(Arg.Any<CancellationToken>())
            .Returns(new[] { localDto }.ToAsyncEnumerable());

        var webhookEvent = new
        {
            topic = "LocalEstoque.Incluido",
            messageId = Guid.NewGuid().ToString(),
            @event = new
            {
                codigo_local_estoque = localOmieId,
                descricao = "Local de Teste Webhook"
            }
        };

        // Act
        var response = await Client.PostAsJsonAsync("/webhook/omie", webhookEvent);

        // Assert
        response.EnsureSuccessStatusCode();

        LocalEstoque? localDB = null;
        var timeout = TimeSpan.FromSeconds(20);
        var start = DateTime.UtcNow;

        while (DateTime.UtcNow - start < timeout)
        {
            using var scope = Factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            localDB = await dbContext.LocaisEstoque
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.OmieId == localOmieId);
            
            if (localDB != null) break;
            await Task.Delay(500);
        }

        localDB.Should().NotBeNull("O local de estoque deve ser sincronizado");
        localDB!.Descricao.Should().Be("Local de Teste Webhook");
    }

    [Fact]
    public async Task Deve_Processar_Webhook_ProdutoEstoque_Com_Debouncing()
    {
        // Arrange
        var produtoOmieId = 777888L;
        var localOmieId = 999111L;

        // Seed: Criar produto e local no banco
        Guid produtoId;
        Guid localId;
        using (var scope = Factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            var local = new LocalEstoque { Id = Guid.NewGuid(), OmieId = localOmieId, Codigo = "LOC1", Descricao = "Local 1" };
            var produto = new Produto { Id = Guid.NewGuid(), OmieId = produtoOmieId, CodigoProduto = "PROD1", Descricao = "Produto 1" };
            
            dbContext.LocaisEstoque.Add(local);
            dbContext.Produtos.Add(produto);
            await dbContext.SaveChangesAsync();
            
            produtoId = produto.Id;
            localId = local.Id;
        }

        // Mock: Resumo de estoque
        var resumo = new ObterEstoqueProdutoResponse
        {
            IdProduto = produtoOmieId,
            ListaEstoque = new List<ResumoEstoqueLocalDto>
            {
                new() { IdLocal = localOmieId, Disponivel = 100, Fisico = 100 }
            }
        };

        Factory.OmieClientMock.ObterResumoEstoqueProdutoAsync(Arg.Is<ObterEstoqueProdutoRequest>(r => r.IdProduto == produtoOmieId), Arg.Any<CancellationToken>())
            .Returns(resumo);

        var webhookEvent = new
        {
            topic = "Produto.MovimentacaoEstoque",
            messageId = Guid.NewGuid().ToString(),
            @event = new
            {
                nCodProd = produtoOmieId,
                codigo_produto = produtoOmieId
            }
        };

        // Act: Enviar DOIS webhooks seguidos para o mesmo produto
        var response1 = await Client.PostAsJsonAsync("/webhook/omie", webhookEvent);
        var response2 = await Client.PostAsJsonAsync("/webhook/omie", webhookEvent);

        // Assert
        response1.EnsureSuccessStatusCode();
        response2.EnsureSuccessStatusCode();

        // Verificar no banco
        ProdutoEstoque? saldoDB = null;
        var timeout = TimeSpan.FromSeconds(20);
        var start = DateTime.UtcNow;

        while (DateTime.UtcNow - start < timeout)
        {
            using var scope = Factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            saldoDB = await dbContext.ProdutosEstoque
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.ProdutoId == produtoId && s.LocalEstoqueId == localId);
            
            if (saldoDB != null && saldoDB.Saldo == 100) break;
            await Task.Delay(500);
        }

        saldoDB.Should().NotBeNull();
        saldoDB!.Saldo.Should().Be(100);

        // Verificação de Debouncing: O Mock deve ter sido chamado exatamente UMA vez (ou pelo menos não duas vezes no curtíssimo intervalo)
        // Como o processamento é assíncrono, aguardamos um pouco mais para garantir que o segundo não rodaria depois
        await Task.Delay(2000); 

        // IMPORTANTE: Aqui validamos que o Debouncing barrou a segunda chamada ao SyncByIdAsync
        await Factory.OmieClientMock.Received(1).ObterResumoEstoqueProdutoAsync(Arg.Any<ObterEstoqueProdutoRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deve_Permitir_Tratar_Movimentacao_Estoque_Negativa()
    {
        // Arrange
        var produtoOmieId = 888999L;
        var localOmieId = 222333L;

        Guid produtoId;
        Guid localId;
        using (var scope = Factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var local = new LocalEstoque { Id = Guid.NewGuid(), OmieId = localOmieId, Codigo = "LOC2", Descricao = "Local 2" };
            var produto = new Produto { Id = Guid.NewGuid(), OmieId = produtoOmieId, CodigoProduto = "PROD2", Descricao = "Produto 2" };
            dbContext.LocaisEstoque.Add(local);
            dbContext.Produtos.Add(produto);
            await dbContext.SaveChangesAsync();
            produtoId = produto.Id;
            localId = local.Id;
        }

        var resumo = new ObterEstoqueProdutoResponse
        {
            IdProduto = produtoOmieId,
            ListaEstoque = new List<ResumoEstoqueLocalDto>
            {
                new() { IdLocal = localOmieId, Disponivel = -50, Fisico = -50 } // Saldo Negativo
            }
        };

        Factory.OmieClientMock.ObterResumoEstoqueProdutoAsync(Arg.Any<ObterEstoqueProdutoRequest>(), Arg.Any<CancellationToken>())
            .Returns(resumo);

        var webhookEvent = new
        {
            topic = "Produto.MovimentacaoEstoque",
            messageId = Guid.NewGuid().ToString(),
            @event = new { nCodProd = produtoOmieId, codigo_produto = produtoOmieId }
        };

        var response = await Client.PostAsJsonAsync("/webhook/omie", webhookEvent);
        response.EnsureSuccessStatusCode();

        ProdutoEstoque? saldoDB = null;
        var timeout = TimeSpan.FromSeconds(10);
        var start = DateTime.UtcNow;

        while (DateTime.UtcNow - start < timeout)
        {
            using var scope = Factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            saldoDB = await dbContext.ProdutosEstoque.FirstOrDefaultAsync(s => s.ProdutoId == produtoId && s.LocalEstoqueId == localId);
            
            if (saldoDB != null && saldoDB.Saldo == -50) break;
            await Task.Delay(500);
        }

        saldoDB.Should().NotBeNull();
        saldoDB!.Saldo.Should().Be(-50, "O banco deve persistir saldo negativo adequadamente.");
    }
}
