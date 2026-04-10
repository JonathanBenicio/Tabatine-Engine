# Auditoria de Domínio: Tabatine.Core vs Cobertura de Integração

> **Referência:** Issue [#25](https://github.com/JonathanBenicio/Tabatine-Engine/issues/25)  
> **Última atualização:** 2026-04-10  
> **Status:** 🟡 Em evolução — cobertura atual estimada em ~60%

---

## Mapa de Cobertura por Entidade

Legenda de status:
- ✅ **Coberta** — Possui pelo menos 1 cenário de teste de integração dedicado  
- 🟡 **Parcial** — Coberta indiretamente (ex: via `AllSyncsIntegrationTests`) mas sem cenários de borda  
- ❌ **Descoberta** — Sem nenhum teste dedicado

---

### 💰 Módulo Financeiro

| Entidade | Arquivo de Entidade | Teste Dedicado | Status | Cenários Faltando |
|----------|-------------------|----------------|--------|-------------------|
| `TituloReceber` | `TituloReceber.cs` | `FinanceiroWebhookIntegrationTests.cs` | ✅ Coberta | Pagamento parcial progressivo |
| `TituloPagar` | `TituloPagar.cs` | `FinanceiroWebhookIntegrationTests.cs` | ✅ Coberta | — |
| `ContaCorrente` | `ContaCorrente.cs` | `ContaCorrenteWebhookIntegrationTests.cs` | ✅ Coberta | Movimentos de crédito/débito |

---

### 📦 Módulo Vendas & Faturamento

| Entidade | Arquivo de Entidade | Teste Dedicado | Status | Cenários Faltando |
|----------|-------------------|----------------|--------|-------------------|
| `PedidoVenda` | `PedidoVenda.cs` | `PedidoWebhookIntegrationTests.cs` | ✅ Coberta | Pedido com >100 itens (volume) |
| `ItemPedido` | `ItemPedido.cs` | `PedidoWebhookIntegrationTests.cs` | 🟡 Parcial | Testes isolados de item (sem pedido pai) |
| `PedidoParcela` | `PedidoParcela.cs` | `PedidoWebhookIntegrationTests.cs` | 🟡 Parcial | Cenário de renegociação de parcelas |
| `NotaFiscal` | `NotaFiscal.cs` | `NotaFiscalWebhookIntegrationTests.cs` | ✅ Coberta | NF com múltiplos itens |
| `ItemNotaFiscal` | `ItemNotaFiscal.cs` | _via_ `NotaFiscalWebhookIntegrationTests` | 🟡 Parcial | Testes de mapeamento de CFOP/NCM |
| `NotaFiscalTitulo` | `NotaFiscalTitulo.cs` | — | ❌ Descoberta | Criar: Vinculação de NF com título financeiro |
| `Vendedor` | `Vendedor.cs` | `VendedorWebhookIntegrationTests.cs` | ✅ Coberta | — |
| `EtapaFaturamento` | `EtapaFaturamento.cs` | `EtapasFormasFaturamentoIntegrationTests.cs` | 🟡 Parcial | Mapeamento via webhook funcional |
| `FormaPagamento` | `FormaPagamento.cs` | — | ❌ Descoberta | Criar: Upsert de formas de pagamento |
| `CondicaoPagamento` | `CondicaoPagamento.cs` | — | ❌ Descoberta | Criar: Sincronização de condições de pagamento |
| `MeioPagamento` | `MeioPagamento.cs` | — | ❌ Descoberta | Criar: Testes básicos de CRUD |

---

### 🏭 Módulo Catálogo & Estoque

| Entidade | Arquivo de Entidade | Teste Dedicado | Status | Cenários Faltando |
|----------|-------------------|----------------|--------|-------------------|
| `Produto` | `Produto.cs` | `ProdutoWebhookIntegrationTests.cs` | ✅ Coberta | Produto com variações de preço por tabela |
| `ProdutoEstoque` | `ProdutoEstoque.cs` | `EstoqueWebhookIntegrationTests.cs` | ✅ Coberta | Teste de volume (1000+ registros streaming) |
| `LocalEstoque` | `LocalEstoque.cs` | `EstoqueWebhookIntegrationTests.cs` | ✅ Coberta | Múltiplos locais simultâneos |

---

### 🏦 Infraestrutura & Auxiliares

| Entidade | Arquivo de Entidade | Teste Dedicado | Status | Cenários Faltando |
|----------|-------------------|----------------|--------|-------------------|
| `Cliente` | `Cliente.cs` | `ClienteWebhookIntegrationTests.cs` | ✅ Coberta | CNPJ inválido / cliente PF vs PJ |
| `Banco` | `Banco.cs` | _via_ `AllSyncsIntegrationTests` | 🟡 Parcial | Criar: `BancoWebhookIntegrationTests.cs` |
| `WebhookEvent` | `WebhookEvent.cs` | _via_ `SystemManualSyncWebhookIntegrationTests` | 🟡 Parcial | DLQ retry completo, dead-letter com max-retry |
| `IntegrationSyncState` | `IntegrationSyncState.cs` | _via_ `SyncManagerIntegrationTests` | 🟡 Parcial | Cursor de sync com data retroativa |
| `Notification` | `Notification.cs` | — | ❌ Descoberta | Criar: Alerta de falha de sync |
| `LogEntry` | `LogEntry.cs` | — | ❌ Descoberta | Criar: Teste de gravação e consulta de logs |
| `Perfil` | `Perfil.cs` | — | ❌ Descoberta | Fora de escopo da sincronização Omie |
| `SyncLock` | `SyncLock.cs` | — | ❌ Descoberta | Criar: Teste de lock distribuído (race condition) |
| `OmieEntityBase` | `OmieEntityBase.cs` | _implícito_ | 🟡 Parcial | Base class — coberta indiretamente |

---

## Resumo Executivo

| Status | Contagem | % |
|--------|----------|---|
| ✅ Coberta | 10 | 45% |
| 🟡 Parcial | 9 | 41% |
| ❌ Descoberta | 6 | 27% |
| **Total** | **22*** | — |

> *Somente entidades de negócio (excluídas: `OmieEntityBase`, `SyncLock`, `Perfil`)

**Cobertura total estimada:** ~60% (considerando parciais como 50%)

---

## Plano de Ataque por Sprint

### Sprint 1 — Alta Prioridade Financeira
**Meta:** +5% de cobertura (foco em fluxos críticos de receita)

| Tarefa | Entidade | Tipo | Esforço |
|--------|----------|------|---------|
| Criar `NotaFiscalTituloIntegrationTests.cs` | `NotaFiscalTitulo` | Novo teste | Médio |
| Adicionar cenário de pagamento parcial progressivo | `TituloReceber` | Borda | Baixo |
| Criar cenário de movimentos de crédito/débito em CC | `ContaCorrente` | Borda | Médio |

### Sprint 2 — Cobertura Auxiliar e Catálogo
**Meta:** +5% de cobertura

| Tarefa | Entidade | Tipo | Esforço |
|--------|----------|------|---------|
| Criar `BancoWebhookIntegrationTests.cs` | `Banco` | Novo arquivo | Baixo |
| Criar testes de `FormaPagamento` e `CondicaoPagamento` | `FormaPagamento`, `CondicaoPagamento` | Novo arquivo | Médio |
| Criar teste de volume: Estoque com 1000+ registros | `ProdutoEstoque` | Stress | Alto |

### Sprint 3 — Resiliência e Infraestrutura
**Meta:** +5% de cobertura

| Tarefa | Entidade | Tipo | Esforço |
|--------|----------|------|---------|
| Criar teste de lock distribuído | `SyncLock` | Novo arquivo | Alto |
| Criar teste de DLQ retry completo | `WebhookEvent` | Borda | Médio |
| Criar teste de cursor de sync retroativo | `IntegrationSyncState` | Borda | Baixo |

---

## Sub-Issues Criadas

Para cada Sprint acima, foram (ou devem ser) criadas issues individuais de implementação. Referência cruzada:

- **Sprint 1** → Issues filhas da #25 (Financeiro avançado)
- **Sprint 2** → Issues filhas da #25 (Catálogo auxiliar)
- **Sprint 3** → Issues filhas da #25 (Resiliência)

---

## Como Executar a Auditoria Localmente

```bash
# Rodar todos os testes e gerar cobertura
dotnet test src/Tabatine.Worker.IntegrationTests \
  --collect:"XPlat Code Coverage" \
  --results-directory ./coverage

# Gerar relatório HTML (requer ReportGenerator - Issue #24)
reportgenerator -reports:coverage/**/*.xml -targetdir:coverage/report -reporttypes:Html
```
