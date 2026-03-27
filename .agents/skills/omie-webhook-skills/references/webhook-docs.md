# Documentação do Processador de Webhooks (Omie)

Este documento detalha a arquitetura e a implementação do recebimento e processamento de eventos via Webhook provenientes da API Omie no **Tabatine Engine**.

## 1. Arquitetura "Fast Acknowledge"

A fila principal da Omie exige que o endpoint retorne um código de resposta `HTTP 200 OK` **imediatamente** (timeout de ~7 segundos). Se o servidor atrasar processando as chamadas e consultas ao banco ou envio de e-mails, ocorrerá um timeout na Omie e o webhook entrará na fila de falhas (*Dead Letter Queue* - DLQ).

Para evitar isto, o fluxo do Tabatine Engine é **totalmente assíncrono**:
1. O endpoint `/webhook/omie` recebe o payload POST.
2. Os dados de entrada são desativados de regras de negócio pesadas.
3. A string JSON pura e o tópico (`Event`) são salvos diretamente em uma tabela do PostgreSQL genérica (`WebhookEvents`) com `Status = 'Pending'`.
4. O endpoint retorna imediatamente o `HTTP 200 OK` para o servidor da Omie.

## 2. Processamento em Background 

O processamento das lógicas de negócio é feito separadamente por um Worker rodando em segundo plano: **`WebhookProcessorWorker`**.

- **Concorrência Segura (`FOR UPDATE SKIP LOCKED`)**: Para permitir múltiplas réplicas do Worker rodando em paralelo sem risco de processamento duplicado no mesmo webhook, a fila no banco de dados é consumida com o uso do bloqueio de nível de linha (`SKIP LOCKED`). O worker consome o próximo evento pendente, trava especificamente aquela linha, processa-a, e commita a transação trocando o status para `Processed` ou `Failed`.

## 3. O Padrão Strategy (Roteamento de Eventos)

A Omie dispara perto de 80 tipos diferentes de eventos (Ex: `VendaProduto.Novo`, `ClienteFornecedor.Incluido`, `NFe.NotaAutorizada`, `CRM.Oportunidade.Faturado`). Em vez de empilhar todas essas verificações em blocos de `if/else`, foi adotado um **Strategy Pattern**.

Foi definida uma interface unificada para manipuladores:
```csharp
public interface IWebhookEventHandler
{
    IEnumerable<string> SupportedEvents { get; }
    Task HandleAsync(WebhookEvent webhookEvent, CancellationToken cancellationToken);
}
```

E uma Factory (`WebhookHandlerFactory`) que mapeia a string do banco de dados (Ex: `"VendaProduto.Novo"`) à sua classe gerenciadora (Ex: `PedidoWebhookHandler`).

### Manipuladores Padrão Existentes

* **`PedidoWebhookHandler`**: Focado em fluxos de venda (`VendaProduto.Incluida`, `VendaProduto.Novo`, `VendaProduto.Cancelada`, etc.). Extrai o Id do ERP em JSON e sincroniza o Pedido da Omie de forma completa.
* **`NotaFiscalWebhookHandler`**: Focado no Faturamento (`Faturamento.NotaFiscalEmitida`, `NFe.NotaAutorizada`, etc.).
* **`ClienteWebhookHandler`**: Lida com `ClienteFornecedor.Incluido`, `ClienteFornecedor.Alterado`, etc.
* **`ProdutoWebhookHandler`**: Processa movimentações de estoque e edições gerais no cadastro de itens (`Produto.Incluido`, `Produto.MovimentacaoEstoque`).
* **`VendedorWebhookHandler`**: Lida com `Vendedor.Incluido`, `Vendedor.Alterado`, `Vendedor.Excluido`.
* **`ContaCorrenteWebhookHandler`**: Lida com eventos financeiros de `Financas.ContaCorrente.Incluido`, `Financas.ContaCorrente.Alterado`, etc.
* **`DefaultWebhookHandler`**: Este é o manipulador global "Fall-back". Caso um evento não mapeado caia na fila (ex: `TabelaPreco.Alterada`), ele registrará no log central a falta do manipulador sem quebrar ou pendurar a fila. O registro no DB será modificado para `Processed` mesmo assim para não obstruir e causar *Retry Loops*.

## 4. Como Adicionar um Novo Webhook

Caso precise adicionar a inteligência de processamento para um novo tópico de Webhook (por exemplo, `OrdemServico.Incluida` ou algum módulo novo), siga este guia:

### Passo 1: O Serviço de Entidade
Certifique-se de você ter um serviço compatível (Ex: um `OrdemServicoSyncService`) capaz de receber um ID e consultar/atualizar este dado.

### Passo 2: Criar o Handler
Crie um novo arquivo/classe implementando a interface `IWebhookEventHandler` na pasta `Tabatine.Worker/Services/Handlers/`.

```csharp
using Tabatine.Worker.Services.Handlers;

public class OrdemServicoWebhookHandler(OrdemServicoSyncService osSync) : IWebhookEventHandler
{
    public IEnumerable<string> SupportedEvents => new[]
    {
        "OrdemServico.Incluida",
        "OrdemServico.Alterada"
    };

    public async Task HandleAsync(WebhookEvent webhookEvent, CancellationToken cancellationToken)
    {
        // 1. Validar Payload, Exemplo: using var doc = JsonDocument.Parse(webhookEvent.Payload);
        // 2. Extrair Código ID do JsonNode
        // 3. Executar o Business Logic (ex: await osSync.SyncByIdAsync(id));
    }
}
```

### Passo 3: Registrar na Injeção de Dependência
Abra o arquivo `Program.cs` ou `ServiceCollectionExtensions.cs` e avise na etapa de `AddScoped` a existência de um novo tratador:

```csharp
// Em ServiceCollectionExtensions.AddOmieInfrastructure()
services.AddScoped<IWebhookEventHandler, OrdemServicoWebhookHandler>();
```

Ao iniciar, a sua nova classe `OrdemServicoWebhookHandler` estará disponível para o `WebhookHandlerFactory` invocar sua nova regra quando necessário!
