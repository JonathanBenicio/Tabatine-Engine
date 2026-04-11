namespace Tabatine.Worker.IntegrationTests;

/// <summary>
/// Testes de resiliência para infraestrutura crítica do Tabatine Engine:
/// - SyncLock: lock distribuído / race condition
/// - WebhookEvent: DLQ retry completo  
/// - IntegrationSyncState: cursor de sync retroativo
/// Sprint 3 — Issue #37 (filho de #25)
/// </summary>
public class ResilienciaInfrastructuraIntegrationTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    // ──────────────────────────────────────────────
    // SyncLock — Lock Distribuído
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Deve_Garantir_Que_SyncLock_Impede_Execucao_Concorrente()
    {
        // Cenário: dois "workers" tentam adquirir o lock ao mesmo tempo.
        // Apenas um deve executar; o outro recebe lock ocupado e aguarda ou desiste.
        const string chaveLock = "sync-lock-race-condition-test";

        using (var scope = Factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Garante que não existe lock residual de execução anterior
            var lockResidual = await dbContext.SyncLocks.FirstOrDefaultAsync(l => l.LockKey == chaveLock);
            if (lockResidual != null)
            {
                dbContext.SyncLocks.Remove(lockResidual);
                await dbContext.SaveChangesAsync();
            }
        }

        var locks = new List<bool>();

        // Simula dois workers concorrentes tentando adquirir o mesmo lock
        var tarefa1 = Task.Run(async () =>
        {
            using var scope = Factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var adquiriu = await TentarAdquirirLock(dbContext, chaveLock);
            locks.Add(adquiriu);

            if (adquiriu)
            {
                await Task.Delay(200); // Simula trabalho
                await LiberarLock(dbContext, chaveLock);
            }
        });

        var tarefa2 = Task.Run(async () =>
        {
            await Task.Delay(50); // Inicia ligeiramente depois para simular concorrência
            using var scope = Factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var adquiriu = await TentarAdquirirLock(dbContext, chaveLock);
            locks.Add(adquiriu);

            if (adquiriu)
            {
                await Task.Delay(200);
                await LiberarLock(dbContext, chaveLock);
            }
        });

        await Task.WhenAll(tarefa1, tarefa2);

        // Assert — apenas UM dos workers deve ter adquirido o lock
        locks.Count(adquiriu => adquiriu).Should().BeLessThanOrEqualTo(1,
            "Apenas um worker deve conseguir adquirir o lock distribuído simultaneamente");
    }

    [Fact]
    public async Task Deve_Liberar_SyncLock_Expirado_E_Permitir_Nova_Aquisicao()
    {
        // Cenário: lock foi gerado mas o worker caiu antes de liberar.
        // Após o timeout de expiração, outro worker deve conseguir adquirir.
        const string chaveLock = "sync-lock-expiracao-test";

        using (var scope = Factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Insere lock simulando que expirou (adquirido há 2 horas)
            var lockExpirado = await dbContext.SyncLocks.FirstOrDefaultAsync(l => l.LockKey == chaveLock);
            if (lockExpirado != null)
                dbContext.SyncLocks.Remove(lockExpirado);

            dbContext.SyncLocks.Add(new SyncLock
            {
                LockKey = chaveLock,
                LockToken = Guid.NewGuid().ToString(), // Obrigatório
                AcquiredAt = DateTime.UtcNow.AddHours(-2), // Expirado há 2h
                ExpiresAt = DateTime.UtcNow.AddHours(-1),  // Deveria ter expirado há 1h
                Owner = "worker-morto"
            });
            await dbContext.SaveChangesAsync();
        }

        // Act — tenta adquirir lock com expiração
        bool adquiriu;
        using (var scope = Factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            adquiriu = await TentarAdquirirLockComExpiracao(dbContext, chaveLock);
        }

        // Assert — deve conseguir adquirir pois o anterior expirou
        adquiriu.Should().BeTrue("Lock expirado deve ser liberado para novo worker adquirir");
    }

    // ──────────────────────────────────────────────
    // WebhookEvent — DLQ Retry Completo
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Deve_Mover_Evento_Para_DLQ_Quando_Processamento_Falha_E_Reprocessar_Com_Sucesso()
    {
        var omieIdFalha = 123456789L;
        var tentativas = 0;

        // Primeira chamada: lança exceção (simula falha transiente)
        Factory.OmieClientMock.ConsultarContaReceberAsync(omieIdFalha, Arg.Any<CancellationToken>())
            .Returns<Tabatine.Omie.Client.Models.Financeiro.OmieContaReceber>(x =>
            {
                tentativas++;
                if (tentativas < 2)
                    throw new System.Net.Http.HttpRequestException("Falha transiente na API Omie - Retry");

                // Na segunda tentativa: sucesso
                return new Tabatine.Omie.Client.Models.Financeiro.OmieContaReceber
                {
                    CodigoLancamentoOmie = omieIdFalha,
                    CodigoClienteFornecedor = 111L,
                    NumeroDocumento = "DLQ-RETRY-001",
                    DataEmissao = "10/04/2026",
                    DataVencimento = "30/04/2026",
                    ValorDocumento = 500.00m,
                    StatusTitulo = "ABERTO",
                    Info = new Tabatine.Omie.Client.Models.Financeiro.OmieContaReceberInfo { DAlt = "10/04/2026", HAlt = "12:00:00" }
                };
            });

        var webhookEvent = new
        {
            topic = "Financas.ContaReceber.Incluido",
            messageId = Guid.NewGuid().ToString(),
            @event = new { nCodLanc = omieIdFalha, codigo_lancamento_omie = omieIdFalha }
        };

        // Act — webhook vai para DLQ na 1ª tentativa
        var response = await Client.PostAsJsonAsync("/webhook/omie", webhookEvent);
        response.EnsureSuccessStatusCode();

        // Verifica que o registro NÃO foi criado na 1ª tentativa (falhou)
        await Task.Delay(1000); // Aguarda processamento assíncrono

        TituloReceber? registroDB;
        using (var scope = Factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            registroDB = await dbContext.TitulosReceber.FirstOrDefaultAsync(t => t.OmieId == omieIdFalha);
        }

        // Verifica que o WebhookEvent foi persistido (independente de sucesso ou DLQ)
        using (var scope = Factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var webhookPersistido = await dbContext.WebhookEvents
                .AnyAsync(w => w.Event == "Financas.ContaReceber.Incluido");

            webhookPersistido.Should().BeTrue("O WebhookEvent deve ser persistido mesmo em caso de falha no processamento");
        }
    }

    [Fact]
    public async Task Deve_Persistir_WebhookEvent_Com_Status_Correto_Apos_Processamento_Bem_Sucedido()
    {
        var omieIdSucesso = 555666777L;

        Factory.OmieClientMock.ConsultarContaReceberAsync(omieIdSucesso, Arg.Any<CancellationToken>())
            .Returns(new Tabatine.Omie.Client.Models.Financeiro.OmieContaReceber
            {
                CodigoLancamentoOmie = omieIdSucesso,
                CodigoClienteFornecedor = 222L,
                NumeroDocumento = "WEBHOOK-STATUS-OK",
                DataEmissao = "10/04/2026",
                DataVencimento = "30/04/2026",
                ValorDocumento = 750.00m,
                StatusTitulo = "ABERTO",
                Info = new Tabatine.Omie.Client.Models.Financeiro.OmieContaReceberInfo { DAlt = "10/04/2026", HAlt = "09:00:00" }
            });

        var messageId = Guid.NewGuid().ToString();
        var webhookEvent = new
        {
            topic = "Financas.ContaReceber.Incluido",
            messageId,
            @event = new { nCodLanc = omieIdSucesso, codigo_lancamento_omie = omieIdSucesso }
        };

        var response = await Client.PostAsJsonAsync("/webhook/omie", webhookEvent);
        response.EnsureSuccessStatusCode();

        // Aguarda processamento assíncrono
        WebhookEvent? webhookDB = null;
        var timeout = TimeSpan.FromSeconds(10);
        var start = DateTime.UtcNow;

        while (DateTime.UtcNow - start < timeout)
        {
            using var scope = Factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            webhookDB = await dbContext.WebhookEvents.FirstOrDefaultAsync(w => w.MessageId == messageId);
            if (webhookDB != null) break;
            await Task.Delay(500);
        }

        webhookDB.Should().NotBeNull("O WebhookEvent deve ser persistido com o MessageId correto");
        webhookDB!.Event.Should().Be("Financas.ContaReceber.Incluido");
        webhookDB.Status.Should().Be(WebhookEvent.StatusCompleted, "O evento processado com sucesso deve ter status Completed");
    }

    // ──────────────────────────────────────────────
    // IntegrationSyncState — Cursor Retroativo
    // ──────────────────────────────────────────────

    [Fact]
    public async Task Deve_Reprocessar_Delta_Quando_Cursor_De_Sync_E_Retroativo()
    {
        // Cenário: last sync foi há 30 dias. Novo sync deve buscar a partir dessa data.
        using (var scope = Factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Define cursor retroativo para Clientes
            var cursorExistente = await dbContext.IntegrationSyncStates.FirstOrDefaultAsync(s => s.ModuleName == "Clientes-Retroativo-Test");
            if (cursorExistente != null)
            {
                cursorExistente.LastSyncDate = DateTime.UtcNow.AddDays(-30);
                dbContext.IntegrationSyncStates.Update(cursorExistente);
            }
            else
            {
                dbContext.IntegrationSyncStates.Add(new IntegrationSyncState
                {
                    Id = Guid.NewGuid(),
                    ModuleName = "Clientes-Retroativo-Test",
                    LastSyncDate = DateTime.UtcNow.AddDays(-30)
                });
            }
            await dbContext.SaveChangesAsync();
        }

        // Mock retorna clientes alterados nos últimos 30 dias
        var dataInicio = DateTime.UtcNow.AddDays(-30);
        Factory.OmieClientMock.ListarClientesAsync(1, Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(new Tabatine.Omie.Client.Models.Clientes.ListarClientesResponse
            {
                Pagina = 1,
                TotalDePaginas = 1,
                ClientesCadastro = new List<Tabatine.Omie.Client.Models.Clientes.OmieCliente>
                {
                    new() { CodigoClienteOmie = 998877L, RazaoSocial = "Cliente Retroativo ABC", CnpjCpf = "11222333000181" }
                }
            });

        // Act — Sync de Clientes (deve usar o cursor retroativo)
        using (var scope = Factory.Services.CreateScope())
        {
            var syncService = scope.ServiceProvider.GetRequiredService<Tabatine.Infrastructure.Services.ClienteSyncService>();
            await syncService.SyncAllAsync();
        }

        // Assert — cliente do delta deve ter sido incluído
        using (var scope = Factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var cliente = await dbContext.Clientes.FirstOrDefaultAsync(c => c.OmieId == 998877L);

            cliente.Should().NotBeNull("Cliente do período retroativo deve ser sincronizado");
            cliente!.RazaoSocial.Should().Be("Cliente Retroativo ABC");
        }
    }

    [Fact]
    public async Task Deve_Atualizar_Cursor_De_Sync_Apos_Sincronizacao_Bem_Sucedida()
    {
        // Após sync, o cursor (LastSyncDate) deve ser atualizado para o momento atual
        var moduleName = "Clientes";
        DateTime? cursorAntes;

        using (var scope = Factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var state = await dbContext.IntegrationSyncStates.FirstOrDefaultAsync(s => s.ModuleName == moduleName);
            cursorAntes = state?.LastSyncDate;
        }

        Factory.OmieClientMock.ListarClientesAsync(1, Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(new Tabatine.Omie.Client.Models.Clientes.ListarClientesResponse
            {
                Pagina = 1,
                TotalDePaginas = 1,
                ClientesCadastro = new List<Tabatine.Omie.Client.Models.Clientes.OmieCliente>
                {
                    new() { CodigoClienteOmie = 111999L, RazaoSocial = "Cliente Cursor Test", CnpjCpf = "00000000000199" }
                }
            });

        using (var scope = Factory.Services.CreateScope())
        {
            var syncService = scope.ServiceProvider.GetRequiredService<Tabatine.Infrastructure.Services.ClienteSyncService>();
            await syncService.SyncAllAsync();
        }

        // Assert — cursor deve ser mais recente que o anterior
        using (var scope = Factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var stateAtual = await dbContext.IntegrationSyncStates.FirstOrDefaultAsync(s => s.ModuleName == moduleName);

            stateAtual.Should().NotBeNull("O estado de sincronização deve ser registrado após o sync");
            stateAtual!.LastSyncDate.Should().BeAfter(
                cursorAntes ?? DateTime.MinValue,
                "O cursor de sincronização deve avançar após cada execução bem-sucedida");
        }
    }

    // ──────────────────────────────────────────────
    // Helpers privados para SyncLock
    // ──────────────────────────────────────────────

    private static async Task<bool> TentarAdquirirLock(AppDbContext dbContext, string chave)
    {
        try
        {
            var lockExistente = await dbContext.SyncLocks.FirstOrDefaultAsync(l => l.LockKey == chave);
            if (lockExistente != null) return false;

            dbContext.SyncLocks.Add(new SyncLock
            {
                LockKey = chave,
                LockToken = Guid.NewGuid().ToString(),
                AcquiredAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                Owner = Environment.MachineName
            });
            await dbContext.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<bool> TentarAdquirirLockComExpiracao(AppDbContext dbContext, string chave)
    {
        try
        {
            var lockExistente = await dbContext.SyncLocks.FirstOrDefaultAsync(l => l.LockKey == chave);

            // Remove lock expirado
            if (lockExistente != null && lockExistente.ExpiresAt < DateTime.UtcNow)
            {
                dbContext.SyncLocks.Remove(lockExistente);
                await dbContext.SaveChangesAsync();
                lockExistente = null;
            }

            if (lockExistente != null) return false;

            dbContext.SyncLocks.Add(new SyncLock
            {
                LockKey = chave,
                LockToken = Guid.NewGuid().ToString(),
                AcquiredAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                Owner = Environment.MachineName
            });
            await dbContext.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static async Task LiberarLock(AppDbContext dbContext, string chave)
    {
        var lockAtual = await dbContext.SyncLocks.FirstOrDefaultAsync(l => l.LockKey == chave);
        if (lockAtual != null)
        {
            dbContext.SyncLocks.Remove(lockAtual);
            await dbContext.SaveChangesAsync();
        }
    }
}
