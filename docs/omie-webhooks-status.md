# Status de Implementação de Webhooks Omie

Este documento detalha quais eventos de Webhook da Omie estão atualmente implementados e processados pelo **Tabatine Engine**, bem como os handlers responsáveis pela lógica de sincronização.

> [!NOTE]
> Eventos não mapeados são capturados pelo `DefaultWebhookHandler`, que registra o log do recebimento mas não executa nenhuma ação de sincronização, marcando o evento como processado para evitar loops na fila.

## 🟢 Eventos Implementados

Estes eventos possuem handlers específicos que disparam a sincronização atômica (por ID) das entidades correspondentes.

| Categoria | Tópicos | Handler Responsável | Observações |
| :--- | :--- | :--- | :--- |
| **Clientes** | `ClienteFornecedor.Incluido`, `ClienteFornecedor.Alterado`, `ClienteFornecedor.Excluido` | [ClienteWebhookHandler](file:///src/Tabatine.Worker/Services/Handlers/ClienteWebhookHandler.cs) | Sincroniza cadastro completo. |
| **Financeiro (Contas)** | `Financas.ContaCorrente.Incluido`, `Financas.ContaCorrente.Alterado`, `Financas.ContaCorrente.Excluido` | [ContaCorrenteWebhookHandler](file:///src/Tabatine.Worker/Services/Handlers/ContaCorrenteWebhookHandler.cs) | Sincroniza saldo e dados da conta. |
| **Financeiro (Títulos)** | `Financas.ContaReceber.Incluido`, `Financas.ContaReceber.Alterado`, `Financas.ContaReceber.Excluido` | [ContasReceberWebhookHandler](file:///src/Tabatine.Worker/Services/Handlers/ContasReceberWebhookHandler.cs) | Sincroniza títulos e status de faturamento. |
| **Financeiro (Títulos)** | `Financas.ContaPagar.Incluido`, `Financas.ContaPagar.Alterado`, `Financas.ContaPagar.Excluido` | [ContasPagarWebhookHandler](file:///src/Tabatine.Worker/Services/Handlers/ContasPagarWebhookHandler.cs) | Sincroniza títulos e status de pagamento. |
| **Faturamento (NFe)** | `NFe.NotaAutorizada`, `NFe.NotaCancelada`, `NFe.NotaDevolucaoAutorizada` | [NotaFiscalWebhookHandler](file:///src/Tabatine.Worker/Services/Handlers/NotaFiscalWebhookHandler.cs) | |
| **Faturamento (NFSe)**| `NFSe.NotaAutorizada`, `NFSe.NotaCancelada`, `NFSe.NotaSubstituida` | [NotaFiscalWebhookHandler](file:///src/Tabatine.Worker/Services/Handlers/NotaFiscalWebhookHandler.cs) | |
| **Vendas** | `VendaProduto.Incluida`, `VendaProduto.Alterada`, `VendaProduto.Cancelada`, `VendaProduto.EtapaAlterada`, `VendaProduto.Faturada` | [PedidoWebhookHandler](file:///src/Tabatine.Worker/Services/Handlers/PedidoWebhookHandler.cs) | |
| **Produtos** | `Produto.Incluido`, `Produto.Alterado`, `Produto.Excluido` | [ProdutoWebhookHandler](file:///src/Tabatine.Worker/Services/Handlers/ProdutoWebhookHandler.cs) | Sincroniza cadastro completo. |
| **Estoque (Saldo)** | `Produto.MovimentacaoEstoque`, `Produto.AjusteEstoque` | [ProdutoWebhookHandler](file:///src/Tabatine.Worker/Services/Handlers/ProdutoWebhookHandler.cs) | **Debouncing (10s)**: Agrupa múltiplas movimentações. |
| **Estoque (Local)** | `LocalEstoque.Incluido`, `LocalEstoque.Alterado`, `LocalEstoque.Excluido` | [LocalEstoqueWebhookHandler](file:///src/Tabatine.Worker/Services/Handlers/LocalEstoqueWebhookHandler.cs) | Sincroniza depósitos/almoxarifados. |
| **Vendedores** | `Vendedor.Incluido`, `Vendedor.Alterado`, `Vendedor.Excluido` | [VendedorWebhookHandler](file:///src/Tabatine.Worker/Services/Handlers/VendedorWebhookHandler.cs) | |

## 🔴 Eventos Não Implementados (Backlog)

Os eventos abaixo constam na lista de tópicos da Omie mas **não possuem** lógica de processamento no Tabatine Engine no momento.

### Cadastro e Configurações
- `CaracteristicaProduto.*` (Alterada, Excluida, Incluida)
- `Categoria.*` (Alterada, Incluida)
- `Departamento.*` (Alterado, Excluido, Incluido)
- `Projeto.*` (Alterado, Excluido, Incluido)
- `Servico.*` (Alterado, Excluido, Incluido)

### Financeiro Avançado
- `Financas.ContaCorrente.Lancamento.*` (Alterado, Excluido, Incluido)
- `Financas.ContaCorrente.Transferencia.*` (Alterado, Excluido, Incluido)
- `Financas.ContaPagar.Baixas`, `Financas.ContaPagar.Rateios`
- `Financas.ContaReceber.Baixas`, `Financas.ContaReceber.Boletos`, `Financas.ContaReceber.Rateios`

### Suprimentos e Compras
- `CompraProduto.*` (Incluida, Alterada, Cancelada, Encerrada, EtapaAlterada, Excluida)
- `NotaEntrada.*` (Alterada, Cancelada, Concluida, Excluida, Incluida)
- `OrdemProducao.*` (Alterada, Concluida, Excluida, Incluida, Revertida)
- `RecebimentoProduto.*` (Alterado, Concluido, Devolvido, Excluido, Incluido, Revertido)
- `RemessaProduto.*` (Alterada, Cancelada, Devolvida, Excluida, Faturada, Incluida)
- `RequisicaoProduto.*` (Alterada, Excluida, Incluida)

### Outros
- `CRM.*` (Contas, Contatos, Oportunidades, Tarefas)
- `ContratoServico.*` (Incluido, Alterado, Ativado, Cancelado, Excluido, Faturado, Suspenso)
- `Frete.*` (Status, Entregas)
- `OrdemServico.*` (Incluida, Alterada, Cancelada, EtapaAlterada, Excluida, Faturada)
- `TabelaPreco.*` / `TabelaPrecoItem.*`

---

> [!TIP]
> Para adicionar suporte a um novo evento, adicione o tópico ao `SupportedEvents` de um handler existente e implemente a chamada ao `SyncService` correspondente. Se for um módulo novo, crie um novo `IWebhookEventHandler`.
