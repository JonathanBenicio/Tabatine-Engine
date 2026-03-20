---
trigger: always_on
---

# Padrões de Codificação — .NET 10 (Tabatine Engine)

## Convenções de Nomes

- **Namespaces**: Seguir o padrão `Tabatine.<Projeto>.<Pasta>`. EX: `Tabatine.Infrastructure.Services`.
- **Classes e Interfaces**:
  - Iniciais em Maiúsculo (PascalCase).
  - Interfaces começam com `I`. EX: `ISyncService`.
- **Propriedades**: PascalCase (ER: `RazaoSocial`).
- **Campos Privados**: CamelCase com underscore (ER: `_dbContext`).
- **Idiomas**:
  - **Entidades e Negócio**: Usar Português para nomes que refletem o domínio do ERP (EX: `Cliente`, `PedidoVenda`, `ItemPedido`).
  - **Arquitetura e Infra**: Usar Inglês para padrões de projeto (EX: `SyncService`, `DbContext`, `Repository`, `Worker`).

---

## Estrutura de Entidades (EF Core)

- Todas as entidades da Omie devem herdar de `OmieEntityBase`.
- Usar `DataAnnotation` ou `Fluent API` para mapear os nomes das colunas se necessário, especialmente para o Supabase.
- **Campos Obrigatórios**:
  - `Id`: `Guid` (Chave Primária).
  - `OmieId`: `long` (ID numérico vindo da Omie).
  - `CreatedAt`, `UpdatedAt`, `OmieUpdatedAt`.

---

## Log e Monitoramento

- Usar `ILogger<T>` injetado via construtor.
- **Log Levels**:
  - `Information`: Início/fim de processos e marcos importantes (ex: sincronizada página X).
  - `Warning`: Erros recuperáveis ou inconsistências leves.
  - `Error`: Falhas críticas que interrompem a sincronização de um registro ou processo.
- **Table Logs**: Registros críticos de auditoria devem ser salvos na tabela `Logs` do banco de dados (usando `LogEntry`).

---

## Tratamento de API (Omie)

- **Models**: Todos os DTOs de request/response devem estar em `Tabatine.Omie.Client.Models`.
- **Sync Logic**:
  - Sempre buscar o último estado de sincronização via `ISyncStateRepository`.
  - Usar `OmieTimestampHelper` para converter datas da Omie (`DAlt`, `HAlt`) para `DateTime`.
  - Implementar lógica de "Upsert" (verificar se o `OmieId` existe antes de criar).
  - **Dica**: No update, compare o `OmieUpdatedAt` para evitar chamadas redundantes ao banco de dados.

---

## Injeção de Dependência

- Registrar serviços no `Program.cs` do `Tabatine.Worker`.
- Usar `AddScoped` para serviços que dependem de `DbContext`.
- Usar `AddSingleton` para clientes de API que não mantêm estado.
