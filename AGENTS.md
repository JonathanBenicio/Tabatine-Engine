# AGENTS.md - Tabatine Engine

## Build & Test Commands

```bash
# Restore dependencies
dotnet restore

# Build solution
dotnet build

# Build specific project
dotnet build src/Tabatine.Worker/Tabatine.Worker.csproj

# Run the Worker
dotnet run --project src/Tabatine.Worker

# Run with custom config
dotnet run --project src/Tabatine.Worker --configuration Release

# Apply migrations
dotnet ef database update --project src/Tabatine.Infrastructure --startup-project src/Tabatine.Worker

# Create migration
dotnet ef migrations add <Name> --project src/Tabatine.Infrastructure --startup-project src/Tabatine.Worker
```

> **Note**: This project currently has no test suite. Agents should create tests when implementing new features.

---

## Code Style Guidelines

| Methods | PascalCase | `ListarClientesAsync` |

### Global Usings

To keep the codebase clean, each project should have a `GlobalUsings.cs` file in its root. Common namespaces to include:
- `Microsoft.EntityFrameworkCore`
- `Tabatine.Core.Entities`
- `Tabatine.Infrastructure.Data`
- `Tabatine.Infrastructure.Services`
- `Tabatine.Omie.Client.Models`

### Language Rules

- **Domain (Entities)**: Use Portuguese (ERP domain terms)
  - `Cliente`, `PedidoVenda`, `ItemPedido`
- **Architecture/Infrastructure**: Use English
  - `SyncService`, `DbContext`, `Repository`, `Worker`

---

## Entity Framework Core

### Base Entity

All Omie entities must inherit from `OmieEntityBase`:

```csharp
public abstract class OmieEntityBase
{
    public Guid Id { get; set; }           // Internal UUID
    public long OmieId { get; set; }       // Omie numeric ID
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? OmieUpdatedAt { get; set; }  // Last Omie update
}
```

### Required Fields

- `Id`: `Guid` (primary key)
- `OmieId`: `long` (for upsert/sync)
- `CreatedAt`, `UpdatedAt`, `OmieUpdatedAt`

### Database

- Use `public` schema by default.
- **Mandatory snake_case**: All database objects (tables, columns, indexes) MUST use `snake_case`. conversion is handled automatically by `EFCore.NamingConventions`. Do **NOT** use `[Column]` attributes.
- Generate migrations from `Tabatine.Infrastructure`.

### Connectivity (Supabase/Npgsql)

When connecting to Supabase via Pooler (Supavisor) or Direct, use these mandatory flags in the connection string to prevent handshake crashes:
- `Pooling=false` (Mandatory for Supavisor)
- `No Reset On Close=true` (Prevents ObjectDisposedException)
- `GssEncryptionMode=Disable` (Handshake compatibility)

---

## API Omie Integration

### Rate Limits (Mandatory)

- **Max requests**: 240/minute
- **Simultaneous**: max 4 requests
- **Block per ID**: 60s between calls to same ID
- **HTTP 425**: After 10 incorrect requests = 30min block

### Pagination

- Use `registros_por_pagina`: 50-100 (max 100)
- Always use incremental sync with `filtrar_por_data_de`
- Handle empty responses (codes `Client-5113`, `Client-101`)

### Structure

- DTOs in: `Tabatine.Omie.Client.Models`
- Use "Upsert" pattern: check `OmieId` exists before insert

---

## Logging & Error Handling

### Log Levels

| Level | Usage |
|-------|-------|
| `Information` | Process milestones, sync page X |
| `Warning` | Recoverable errors, inconsistencies |
| `Error` | Critical failures stopping sync |

- Use `ILogger<T>` injetado via **Primary Constructor**
- Log critical operations to `LogEntry` table for audit

### Error Handling

- Don't throw for expected flows (use `Result<T>` pattern)
- Implement Circuit Breaker with Polly for API resilience
- Use exponential backoff for retries

---

## Dependency Injection

| Service Type | Lifetime |
|--------------|----------|
| DbContext-dependent | `AddScoped` |
| API Clients (stateless) | `AddSingleton` |
| Configuration | `IOptions<T>` |

---

## Project Structure

```
src/
├── Tabatine.Core/           # Domain entities, interfaces
│   └── Entities/            # POCO classes
├── Tabatine.Infrastructure/ # EF Core, sync services
│   ├── Data/                # DbContext, Configurations
│   └── Services/            # Sync services
├── Tabatine.Omie.Client/    # Omie API client
│   └── Models/              # Request/Response DTOs
├── Tabatine.Worker/         # Background service, endpoints
└── Tabatine.ConnectionTester/ # Connectivity & Handshake validation tool

doc/                         # API Samples, RLS Policies, Architecture docs
├── client/                  # JavaScript/Postman samples
└── supabase/                # SQL migrations & RLS scripts
```

---

## Configuration

Environment variables in `appsettings.json` or `.env`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=...;Database=postgres;..."
  },
  "OmieApi": {
    "AppKey": "...",
    "AppSecret": "..."
  }
}
```

---

## User Profiles & Telegram

### Perfil Entity

Manages system users and their integration with external platforms (Telegram).

- **Table**: `perfis`
- **Fields**: `Id` (PK referencing `auth.users.id`), `Nome` (string), `TelegramChatId` (long), `TelegramLinkToken` (Guid), `UpdatedAt`.

### Telegram Linking

1. **Token Generation**: Generate a `TelegramLinkToken` (valid for 15-30m).
2. **Bot Interaction**: User sends the token to the Telegram Bot.
3. **Webhook Processing**: `TelegramWebhookEndpoints` validates the token and links the `TelegramId` to the User's `Perfil`.

---

## Existing Agent Rules

Refer to these files for core architecture rules:

- `.agents/rules/geral.md` - Project overview & entry point
- `.agents/rules/dot-net-standards.md` - C# 10 standards & Primary Constructors
- `.agents/rules/efcore-supabase-rules.md` - Database & EF Core standards
- `.agents/rules/omie-api-rules.md` - Omie API & Memory Safety (Antigravity Rules)

### Specialized Skills

For specific tasks, view the `SKILL.md` in:
- `.agents/skills/omie-api-skills/` - Omie API Integration & Validation
- `.agents/skills/omie-webhooks/` - Omie Webhooks (Fast Acknowledge & Processing)
- `.agents/skills/omie-webhook-skills/` - Webhook Ingestion Infrastructure

### Workflows

- `.agents/workflows/omie-api-workflow.md` - Omie API integration workflow
- `.agents/workflows/omie-webhook-workflow.md` - Omie webhook handling workflow

---

## Key Patterns

1. **Incremental Sync**: Always use date filters to fetch only changes
2. **Idempotency**: Jobs must be restartable without duplicating data
3. **Distributed Lock**: Use `IDistributedLockService` for multi-container deployments
4. **Health Checks**: Endpoint `/health` monitors DB and Omie connectivity
