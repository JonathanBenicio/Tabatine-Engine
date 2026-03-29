---
name: omie-module-name-skill
description: "Módulo responsável por [descrever a ação principal, ex: sincronizar o cadastro de clientes via API Omie]."
version: "1.0.0"
category: "Integration.ERP"
dependencies: 
  - "omie-http-client-skill"
  - "omie-error-translator-skill"
---

# Skill: [Nome do Módulo - ex: Omie Customer Sync]

## 1. Definition
[Descreva em uma frase o propósito atômico desta Skill. O que ela resolve para o negócio? Evite jargões técnicos aqui. Ex: "Garante que a base local de clientes esteja sempre sincronizada com o ERP Omie, processando listagens e webhooks de alteração."]

## 2. Capabilities
Quais são as ações isoladas que esta Skill pode executar? (Use verbos de ação).

* **[Nome da Capacidade 1 - ex: FetchPaginatedClients]:** Busca registros na Omie utilizando blocos de paginação controlados via `IAsyncEnumerable`.
* **[Nome da Capacidade 2 - ex: ProcessWebhookPayload]:** Recebe e valida o payload de webhooks da Omie para atualizações em tempo real (Partial Sync).
* **[Nome da Capacidade 3 - ex: MapToDomain]:** Isola a conversão estrutural do JSON da Omie (ex: `clientes_cadastro`) para a entidade rica do domínio da plataforma.

## 3. Constraints & Antigravity Rules
Restrições técnicas e regras para evitar acoplamento ou quebra do serviço.

> [!WARNING] Rate Limits
> A Omie limita o processamento a 4 requisições por segundo por AppKey. Esta Skill DEVE obrigatoriamente invocar o `RateLimiter` interno.

> [!IMPORTANT] Fault Tolerance (`PROTO_BYEBYE`)
> Falhas com o código `PROTO_BYEBYE` ou instabilidades de rede (502/504) devem acionar a política de *Exponential Backoff* antes de abortar a sincronização.

* **Isolamento de Domínio:** Nenhuma classe do Core da aplicação deve conhecer propriedades específicas da Omie como `cCodIntCli` ou `nCodCli`.

## 4. Input / Output Contracts
Definição semântica do que entra e do que sai desta Skill (Dumb Payloads na borda).

### Input Clássico (Parâmetros)
* `Since` (DateTime?): Filtra apenas registros alterados após esta data (incremental sync).
* `PageSize` (int): Padrão de 50, máximo de 500 por requisição.

### Output Semântico (Result Pattern)
* `Success`: Retorna um stream (`IAsyncEnumerable<DomainEntity>`) dos registros formatados.
* `Failure`: Retorna um objeto `Error` tipado (ex: `Error.Validation`, `Error.Network`) detalhado pelo *Error Translator*.

## 5. Usage Example (Agent Context)
Exemplo de como o código consumirá esta Skill. (Isso instrui o LLM sobre o padrão arquitetural esperado).

```csharp
// Exemplo de Injeção e Uso da Skill no Application Service
public class CustomerSyncService
{
    private readonly ISyncCustomersSkill _syncSkill;

    public CustomerSyncService(ISyncCustomersSkill syncSkill)
    {
        _syncSkill = syncSkill;
    }

    public async Task ExecuteIncrementalSyncAsync(DateTime lastSync)
    {
        // A Skill encapsula a paginação e a resiliência. O serviço apenas itera.
        var resultStream = _syncSkill.FetchPaginatedClientsAsync(lastSync);

        await foreach (var customer in resultStream)
        {
             // Processamento no domínio (ex: salvar no banco local)
             await _repository.UpsertAsync(customer);
        }
    }
}