# Auditoria de Cobertura de Domínio - Tabatine Engine

Este documento mapeia a relação entre as entidades Omie, seus serviços de sincronização e o estado atual dos testes automatizados.

## 📊 Matriz de Cobertura (Baseline)

| Entidade | Serviço de Sincronização | Teste de Integração (Caminho Feliz) | Teste de Webhook | Teste de Resiliência |
| :--- | :--- | :---: | :---: | :---: |
| **Cliente** | `ClienteSyncService` | ✅ | ✅ | ❌ |
| **PedidoVenda** | `PedidoSyncService` | ✅ | ✅ | ❌ |
| **Produto** | `ProdutoSyncService` | ✅ | ✅ | ❌ |
| **TituloPagar** | `ContasPagarSyncService` | ✅ | ✅ | ❌ |
| **TituloReceber** | `ContasReceberSyncService` | ✅ | ✅ | ❌ |
| **NotaFiscal** | `NotaFiscalSyncService` | ✅ | ✅ | ❌ |
| **ContaCorrente** | `ContaCorrenteSyncService` | ✅ | ✅ | ❌ |
| **Vendedor** | `VendedorSyncService` | ✅ | ✅ | ❌ |
| **Banco** | `BancoSyncService` | ✅ | N/A | ❌ |
| **MeioPagamento**| `MeioPagamentoSyncService`| ✅ | N/A | ❌ |
| **FormasPagamento**| `FormaPagamentoSyncService`| ✅ | N/A | ❌ |
| **EtapasFat** | `EtapaFaturamentoSyncService`| ✅ | N/A | ❌ |
| **LocalEstoque** | `EstoqueSyncService` | ✅ | ✅ | ❌ |
| **ProdutoEstoque**| `EstoqueSyncService` | ✅ | ✅ | ❌ |

---

## 🛠️ Glossário de Status
- ✅ **Coberto**: Existe teste validando a persistência básica no banco.
- ❌ **Não Coberto**: Não há teste específico para este cenário.
- **N/A**: Não se aplica (ex: Tabelas que não possuem Webhooks na Omie).

## 🚀 Próximos Passos (Auditados)
1. **Cobrir Webhooks de Finanças**: Prioridade alta para garantir reconciliação bancária em tempo real.
2. **Implementar Upsert Edge Cases**: Testar o que acontece se um registro for atualizado na Omie com o mesmo ID, mas dados conflitantes.
3. **Simular Throttling**: Testar o comportamento do `Circuit Breaker` quando a API Omie retorna 429.
