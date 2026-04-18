# ADR-005: Resiliência contra Concorrência via Retry Patterns e Jitter

## Status
Accepted

## Contexto
O Tabatine Engine é um sistema orientado a eventos que processa uma alta volumetria de webhooks simultâneos provenientes do Omie ERP. Diferentes eventos podem referenciar e tentar atualizar a mesma entidade (ex: um produto sendo alterado e tendo seu estoque movimentado ao mesmo tempo) ou o status do próprio evento na fila de processamento.

Essas colisões resultavam em `DbUpdateConcurrencyException` frequentes, causando falhas prematuras no processamento e sobrecarga na DLQ (Dead Letter Queue), exigindo intervenção manual ou retentativas custosas.

## Decisão
Para aumentar a resiliência do sistema e garantir a integridade dos dados sob carga, decidimos:

1.  **Retry de Concorrência**: Implementar loops de retentativa específicos para `DbUpdateConcurrencyException` em todos os serviços de sincronização (`ISyncService`) e no worker de processamento de fila.
2.  **Uso de Jitter**: Adotar um tempo de espera aleatório (backoff com jitter) entre as tentativas (ex: 100ms a 500ms). Isso evita que threads colidentes tentem atualizar o registro exatamente no mesmo milissegundo repetidamente.
3.  **Atualização Atômica de Estado**: Na falha de concorrência, o sistema recarrega os valores atuais do banco de dados para a entrada rastreada antes de tentar aplicar as mudanças novamente.

## Consequências
### Positivas
- **Alta Resiliência**: O sistema torna-se capaz de absorver picos de tráfego sem corromper dados ou interromper o serviço.
- **Redução de Erros Falsos**: Diminuição drástica de exceções transientes nos logs de produção.
- **Auto-recuperação**: A maioria das colisões de banco é resolvida automaticamente na segunda ou terceira tentativa.

### Negativas/Trade-offs
- **Complexidade do Código**: Os métodos de persistência agora exigem lógica de loop e tratamento de exceção explícito.
- **Latência Marginal**: Pequeno aumento no tempo de processamento por mensagem quando há colisão, devido ao tempo de espera do jitter.

## Referências
- `src/Tabatine.Worker/Services/WebhookProcessorWorker.cs`
- `src/Tabatine.Infrastructure/Services/ClienteSyncService.cs`
- `src/Tabatine.Infrastructure/Services/ProdutoSyncService.cs`
