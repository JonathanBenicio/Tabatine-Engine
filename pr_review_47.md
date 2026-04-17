# Code Review — PR #47
**`feat: implement infrastructure synchronization services and webhook`**
`bug/pipeline` → `develop/antigravity`

**Última atualização**: 13/04/2026 19:58 (BRT)

---

## Sumário Executivo

O PR consolida a refatoração de **14 SyncServices** para Primary Constructors e adiciona persistência de estado via `ISyncStateRepository`. Todos os issues críticos e menores identificados na revisão original foram corrigidos em commits subsequentes. O PR está **pronto para merge**.

---

## ✅ Pontos Positivos

| # | O que está bem |
|---|---|
| 1 | **Primary Constructors (.NET 10)**: todos os 14 serviços corretamente migrados, sem campos `private readonly` redundantes |
| 2 | **Eliminação de N+1**: `ToDictionaryAsync` com pré-carregamento em batch por página — padrão correto |
| 3 | **Constantes de status centralizadas**: `WebhookEvent.StatusPending/Processing/Completed/Failed/DeadLetter` em `WebhookEvent.cs` — ótima decisão de design |
| 4 | **HashSet para idempotência**: controle de IDs duplicados por ciclo presente na maioria dos serviços |
| 5 | **Guard de `ct.IsCancellationRequested`** antes de salvar cursor — **aplicado a todos os 14 serviços** |
| 6 | **Streaming no `EstoqueSyncService`**: uso correto de `await foreach` com `IAsyncEnumerable` |
| 7 | **`AsNoTracking()`** na fase de de-queue do Worker — evita conflitos de tracking no update final |
| 8 | **Retry seguro no rollback** do `WebhookProcessorWorker`: `catch { }` no rollback para não mascarar a exceção original |
| 9 | **File-scoped namespaces** em conformidade com `AGENTS.md` em todos os serviços |
| 10 | **Migração transparente de SyncKey** em `ContasPagarSyncService` e `ContasReceberSyncService` — evita full sync no deploy |

---

## 🔴 Issues Críticos — ~~Bloquear Merge~~ ✅ Todos Resolvidos

### ~~1. Inconsistência no guard do `ct.IsCancellationRequested`~~  ✅ RESOLVIDO

**Status**: Corrigido em sessões anteriores.

Todos os 14 serviços agora aplicam o guard antes do `SetLastSyncDateAsync`:
```csharp
if (!ct.IsCancellationRequested)
{
    await syncState.SetLastSyncDateAsync(SyncKey, syncStartTime, ct);
}
```

| Serviço | Guard presente? |
|---|---|
| `VendedorSyncService` | ✅ |
| `ClienteSyncService` | ✅ |
| `ProdutoSyncService` | ✅ |
| `ContaCorrenteSyncService` | ✅ |
| `BancoSyncService` | ✅ |
| `CondicaoPagamentoSyncService` | ✅ |
| `EtapaFaturamentoSyncService` | ✅ |
| `FormaPagamentoSyncService` | ✅ |
| `MeioPagamentoSyncService` | ✅ |
| `EstoqueSyncService` | ✅ |
| `ContasPagarSyncService` | ✅ |
| `ContasReceberSyncService` | ✅ |
| `PedidoSyncService` | ✅ |
| `NotaFiscalSyncService` | ✅ |

---

### ~~2. `NotaFiscalSyncService` — Bug lógico no update~~  ✅ RESOLVIDO

**Status**: Verificado em sessão anterior — o `Add(existing)` incorreto **já não existe** no código atual. O update path apenas modifica propriedades da entidade rastreada.

---

### ~~3. `WebhookProcessorWorker` — Log ausente para event null~~  ✅ RESOLVIDO

**Status**: Verificado em sessão anterior — o Worker agora loga `LogError` quando `dbEvent == null` no update final, evitando perda silenciosa de eventos.

---

## 🟡 Issues Menores — ~~Melhorias Recomendadas~~ ✅ Todas Implementadas

### ~~4. `SyncKey` renomeado sem migração de cursor~~  ✅ RESOLVIDO

**Commits**: `fix(#4): migração transparente de cursor ContasPagar → LancamentosPagar` e equivalente para ContasReceber.

**Solução implementada**: Migração transparente no `SyncAllAsync` — ao iniciar, se a nova chave (`LancamentosPagar`/`LancamentosReceber`) não tem cursor, o serviço busca automaticamente a chave legada (`ContasPagar`/`ContasReceber`) e a usa como ponto de partida:

```csharp
private const string SyncKeyLegado = "ContasPagar";

var lastSyncDate = await syncState.GetLastSyncDateAsync(SyncKey, ct);
if (lastSyncDate == null)
{
    var legacyDate = await syncState.GetLastSyncDateAsync(SyncKeyLegado, ct);
    if (legacyDate != null)
    {
        logger.LogInformation("Cursor legado encontrado ({Date}). Migrando.", legacyDate);
        lastSyncDate = legacyDate;
    }
}
```

---

### ~~5. `EstoqueSyncService.SaveChangesSafelyAsync` — descarte do lote inteiro~~  ✅ RESOLVIDO

**Commit**: `fix(#5): EstoqueSyncService - salvar item-por-item em violação de FK`

**Antes**: `ChangeTracker.Clear()` descartava o lote completo (500 saldos) por 1 registro com FK inválida.

**Depois**: O método captura as entidades pendentes, limpa o tracker, e tenta cada uma individualmente. Apenas o(s) registro(s) problemático(s) são descartados:

```csharp
catch (DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException { SqlState: "23503" })
{
    var entries = dbContext.ChangeTracker.Entries()
        .Where(e => e.State is EntityState.Added or EntityState.Modified).ToList();
    dbContext.ChangeTracker.Clear();

    foreach (var entry in entries)
    {
        try { /* re-attach e save individual */ salvos++; }
        catch (DbUpdateException) { dbContext.ChangeTracker.Clear(); descartados++; }
    }
    logger.LogInformation("{Salvos} salvos, {Descartados} descartados por FK inválida.", salvos, descartados);
}
```

---

### ~~6. Teste de integração com `Task.Delay` fixo~~  ✅ RESOLVIDO

**Commit**: `test(#6): remove Task.Delay fixo antes do loop de polling`

O `await Task.Delay(1000)` antes do loop de polling foi removido. O loop já cumpre essa função. O intervalo dentro do loop foi reduzido para **500ms** (mais responsivo).

---

### ~~7. `WebhookProcessorWorker` — Log em event null~~  ✅ RESOLVIDO

Verificado em sessão anterior — já implementado.

---

### ~~8. Parallelização desabilitada globalmente~~  ✅ RESOLVIDO

**Commit**: `test(#8): remove DisableTestParallelization assembly-level`

`[assembly: CollectionBehavior(DisableTestParallelization = true)]` removido do `GlobalUsings.cs`. A collection `"DatabaseCollection"` (via `IntegrationTestCollection.cs`) já isola corretamente as classes que compartilham o container PostgreSQL. Classes sem `[Collection]` agora rodam em paralelo, reduzindo tempo de CI.

---

## 🔧 Conformidade com AGENTS.md

Auditoria final realizada após os commits de correção:

| Regra | Status |
|---|---|
| Primary Constructors (sem `private readonly`) | ✅ |
| File-scoped namespaces (`namespace X;`) | ✅ |
| GlobalUsings — sem usings redundantes | ✅ |
| Sem `[Column]`/`[Table]` (Fluent API only) | ✅ |
| `IAsyncEnumerable` + `await foreach` para streaming | ✅ |
| Guard `ct.IsCancellationRequested` em todos os services | ✅ |
| Sem exceções para controle de fluxo de negócio | ✅ |
| Logging levels corretos (Info/Warning/Debug/Error) | ✅ |
| Comentários de negócio em Português | ✅ |
| Nomes de infraestrutura em Inglês | ✅ |
| Sem XML doc em métodos privados triviais | ✅ |

---

## 📊 Resumo por Arquivo

| Arquivo | Status | Notas |
|---|---|---|
| `WebhookEvent.cs` | ✅ Aprovado | — |
| `WebhookProcessorWorker.cs` | ✅ Aprovado | Logging para null events corrigido |
| `ClienteSyncService.cs` | ✅ Aprovado | — |
| `ProdutoSyncService.cs` | ✅ Aprovado | — |
| `VendedorSyncService.cs` | ✅ Aprovado | — |
| `PedidoSyncService.cs` | ✅ Aprovado | — |
| `NotaFiscalSyncService.cs` | ✅ Aprovado | Bug `Add` vs `Update` já ausente |
| `BancoSyncService.cs` | ✅ Aprovado | Guard adicionado |
| `CondicaoPagamentoSyncService.cs` | ✅ Aprovado | Guard adicionado |
| `EtapaFaturamentoSyncService.cs` | ✅ Aprovado | Guard adicionado |
| `FormaPagamentoSyncService.cs` | ✅ Aprovado | Guard adicionado |
| `MeioPagamentoSyncService.cs` | ✅ Aprovado | Guard adicionado |
| `EstoqueSyncService.cs` | ✅ Aprovado | SaveChangesSafely + file-scoped ns |
| `ContasPagarSyncService.cs` | ✅ Aprovado | Migração de cursor + file-scoped ns |
| `ContasReceberSyncService.cs` | ✅ Aprovado | Migração de cursor + file-scoped ns |
| `ContaCorrenteSyncService.cs` | ✅ Aprovado | — |
| `GlobalUsings.cs` (Tests) | ✅ Aprovado | `DisableTestParallelization` removido |
| `ClienteWebhookIntegrationTests.cs` | ✅ Aprovado | Delay fixo removido |

---

## Veredicto Final

> **✅ Aprovar — Pronto para Merge**

Todos os 3 issues críticos e 5 issues menores foram resolvidos. O código está em conformidade com as regras do `AGENTS.md`, segue os padrões de Primary Constructors, file-scoped namespaces, e as convenções de logging/cancellation do projeto.

### Commits de Correção Aplicados

| Commit | Escopo |
|---|---|
| `test(#8): remove DisableTestParallelization assembly-level` | GlobalUsings.cs |
| `test(#6): remove Task.Delay fixo antes do loop de polling` | ClienteWebhookIntegrationTests.cs |
| `fix(#4): migração transparente de cursor ContasPagar → LancamentosPagar` | ContasPagarSyncService.cs |
| `fix(#4): migração transparente de cursor ContasReceber → LancamentosReceber` | ContasReceberSyncService.cs |
| `fix(#5): EstoqueSyncService - salvar item-por-item em violação de FK` | EstoqueSyncService.cs |
| `refactor: aplicar file-scoped namespace e remover usings redundantes` | ContasPagarSyncService.cs |
| `refactor: aplicar file-scoped namespace e remover usings redundantes` | ContasReceberSyncService.cs |
| `refactor: aplicar file-scoped namespace, remover usings redundantes e XML doc` | EstoqueSyncService.cs |
