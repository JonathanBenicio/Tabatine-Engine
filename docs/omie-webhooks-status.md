# Status de Implementação de Webhooks Omie

Este documento detalha quais eventos de Webhook da Omie estão atualmente implementados e processados pelo **Tabatine Engine**, bem como os handlers responsáveis pela lógica de sincronização.

> [!IMPORTANT]
> Esta lista baseia-se nos tópicos reais suportados pela API Omie. Entidades de catálogo (como Bancos, Meios e Condições de Pagamento) não possuem webhooks e são sincronizadas apenas via carga total ou manual.

## 🟢 Eventos Implementados

Estes eventos possuem handlers específicos que disparam a sincronização atômica (por ID) das entidades correspondentes.

| Categoria | Tópicos | Handler Responsável | Observações |
| :--- | :--- | :--- | :--- |
| **Clientes** | `ClienteFornecedor.Incluido`, `ClienteFornecedor.Alterado`, `ClienteFornecedor.Excluido` | [ClienteWebhookHandler](../src/Tabatine.Worker/Services/Handlers/ClienteWebhookHandler.cs) | Sincroniza cadastro completo. |
| **Financeiro (Contas)** | `Financas.ContaCorrente.Incluido`, `Financas.ContaCorrente.Alterado`, `Financas.ContaCorrente.Excluido` | [ContaCorrenteWebhookHandler](../src/Tabatine.Worker/Services/Handlers/ContaCorrenteWebhookHandler.cs) | Sincroniza saldo e dados da conta. |
| **Financeiro (Títulos)** | `Financas.ContaReceber.Incluido`, `Financas.ContaReceber.Alterado`, `Financas.ContaReceber.Excluido` | [ContasReceberWebhookHandler](../src/Tabatine.Worker/Services/Handlers/ContasReceberWebhookHandler.cs) | Sincroniza títulos e status de faturamento. |
| **Financeiro (Títulos)** | `Financas.ContaPagar.Incluido`, `Financas.ContaPagar.Alterado`, `Financas.ContaPagar.Excluido` | [ContasPagarWebhookHandler](../src/Tabatine.Worker/Services/Handlers/ContasPagarWebhookHandler.cs) | Sincroniza títulos e status de pagamento. |
| **Faturamento (NFe)** | `NFe.NotaAutorizada`, `NFe.NotaCancelada`, `NFe.NotaDevolucaoAutorizada` | [NotaFiscalWebhookHandler](../src/Tabatine.Worker/Services/Handlers/NotaFiscalWebhookHandler.cs) | |
| **Faturamento (NFSe)**| `NFSe.NotaAutorizada`, `NFSe.NotaCancelada`, `NFSe.NotaSubstituida` | [NotaFiscalWebhookHandler](../src/Tabatine.Worker/Services/Handlers/NotaFiscalWebhookHandler.cs) | |
| **Vendas** | `VendaProduto.Incluida`, `VendaProduto.Alterada`, `VendaProduto.Cancelada`, `VendaProduto.EtapaAlterada`, `VendaProduto.Faturada` | [PedidoWebhookHandler](../src/Tabatine.Worker/Services/Handlers/PedidoWebhookHandler.cs) | |
| **Produtos** | `Produto.Incluido`, `Produto.Alterado`, `Produto.Excluido` | [ProdutoWebhookHandler](../src/Tabatine.Worker/Services/Handlers/ProdutoWebhookHandler.cs) | Sincroniza cadastro completo. |
| **Estoque (Saldo)** | `Produto.MovimentacaoEstoque`, `Produto.AjusteEstoque` | [ProdutoWebhookHandler](../src/Tabatine.Worker/Services/Handlers/ProdutoWebhookHandler.cs) | **Debouncing (10s)**: Agrupa múltiplas movimentações. |
| **Estoque (Local)** | `LocalEstoque.Incluido`, `LocalEstoque.Alterado`, `LocalEstoque.Excluido` | [LocalEstoqueWebhookHandler](../src/Tabatine.Worker/Services/Handlers/LocalEstoqueWebhookHandler.cs) | Sincroniza depósitos/almoxarifados. |
| **Vendedores** | `Vendedor.Incluido`, `Vendedor.Alterado`, `Vendedor.Excluido` | [VendedorWebhookHandler](../src/Tabatine.Worker/Services/Handlers/VendedorWebhookHandler.cs) | |

## 🔴 Eventos Existentes mas Não Implementados (Backlog)

Esta lista contém tópicos reais da Omie que ainda não possuem um handler específico no Tabatine Engine.

### Cadastro e Configurações
- `CaracteristicaProduto.*` (Incluida, Alterada, Excluida)
- `Categoria.*` (Incluida, Alterada)
- `Departamento.*` (Incluido, Alterado, Excluido)
- `Projeto.*` (Incluido, Alterado, Excluido)
- `Servico.*` (Incluido, Alterado, Excluido)

### Financeiro e CRM
- `Financas.ContaCorrente.Lancamento.*` (Incluido, Alterado, Excluido)
- `Financas.ContaCorrente.Transferencia.*` (Incluido, Alterado, Excluido)
- `Financas.ContaPagar.BaixaRealizada`, `Financas.ContaPagar.BaixaCancelada`
- `Financas.ContaPagar.Rateio.*` (Categoria, Departamento)
- `Financas.ContaReceber.BaixaRealizada`, `Financas.ContaReceber.BaixaCancelada`, `Financas.ContaReceber.BoletoGerado`, `Financas.ContaReceber.BoletoCancelado`
- `Financas.ContaReceber.Rateio.*` (Categoria, Departamento)
- `CRM.Conta.*`, `CRM.Contato.*`, `CRM.Oportunidade.*`, `CRM.Tarefa.*`

### Suprimentos e Compras
- `CompraProduto.*` (Incluida, Alterada, Cancelada, Encerrada, EtapaAlterada, Excluida)
- `NotaEntrada.*` (Incluida, Alterada, Cancelada, Concluida, Excluida)
- `OrdemProducao.*` (Incluida, Alterada, Concluida, Excluida, Revertida)
- `RecebimentoProduto.*` (Incluido, Alterado, Concluido, Devolvido, Excluido, Revertido)
- `RemessaProduto.*` (Incluida, Alterada, Cancelada, Devolvida, Excluida, Faturada)
- `RequisicaoProduto.*` (Incluida, Alterada, Excluida)

### Outros
- `ContratoServico.*` (Incluido, Alterado, Ativado, Cancelado, Excluido, Faturado, Suspenso)
- `Frete.*` (PedidoEnviado, SaiuParaEntrega, PedidoEntregue, StatusAtualizado)
- `OrdemServico.*` (Incluida, Alterada, Cancelada, EtapaAlterada, Excluida, Faturada)
- `TabelaPreco.*` / `TabelaPrecoItem.*`
