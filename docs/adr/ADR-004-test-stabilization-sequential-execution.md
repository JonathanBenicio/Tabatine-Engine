# ADR-004: Estabilização de Testes de Integração via Execução Sequencial

## Status
Accepted

## Contexto
O Tabatine Engine utiliza um banco de dados compartilhado (via Testcontainers) para sua suíte de testes de integração. Cada teste, ao iniciar, utiliza o `Respawner` para resetar o estado do banco. Paralelamente, o `WebApplicationFactory` dispara workers em background (`WebhookProcessorWorker`) que tentam processar mensagens de webhooks persistidas durante a fase de "Act" do teste.

Essa arquitetura assíncrona gerava *race conditions* frequentes: o Teste B resetava o banco enquanto o Worker do Teste A ainda tentava atualizar o status final de um webhook, resultando em falhas intermitentes de "Null Reference" ou "Record Not Found" nos asserts e logs repletos de exceções de concorrência.

## Decisão
Para garantir 100% de determinismo e estabilidade nos testes, decidimos:

1.  **Desativar Paralelismo**: Configurar o xUnit via `xunit.v3.json` para desativar a execução paralela de testes que compartilham o mesmo banco de dados.
2.  **Processamento Síncrono de Webhooks**: Desativar o loop automático do `WebhookProcessorWorker` durante os testes (via configuração `WebhookProcessor:Enabled=false`) e introduzir o utilitário `ProcessWebhooksAsync` no `BaseIntegrationTest`.
3.  **Controle Manual**: Os testes agora disparam o processamento de forma manual e síncrona após o recebimento do webhook, aguardando a conclusão da tarefa antes de realizar os asserts no banco de dados.

## Consequências
### Positivas
- **Estabilidade Total**: Eliminação de falhas intermitentes (flakiness) na suíte de testes.
- **Depuração Facilitada**: O fluxo de execução do teste torna-se linear e previsível.
- **Logs Limpos**: Redução drástica de ruído de exceções de concorrência e erros de banco nos logs de teste.

### Negativas/Trade-offs
- **Aumento no Tempo de Execução**: A execução sequencial é inerentemente mais lenta que a paralela. No entanto, o tempo total é compensado pela ausência de falhas que exigiam múltiplas execuções da suite.
- **Acoplamento Técnico**: Os testes agora precisam chamar explicitamente o utilitário de processamento.

## Referências
- `src/Tabatine.Worker.IntegrationTests/xunit.v3.json`
- `src/Tabatine.Worker.IntegrationTests/BaseIntegrationTest.cs` (método `ProcessWebhooksAsync`)
