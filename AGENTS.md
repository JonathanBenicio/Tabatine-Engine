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

### Naming Conventions

| Element | Convention | Example |
|---------|------------|---------|
| Namespaces | `Tabatine.<Projeto>.<Pasta>` | `Tabatine.Infrastructure.Services` |
| Classes/Interfaces | PascalCase | `ClienteSyncService`, `IOmieClient` |
| Interfaces | Start with `I` | `ISyncService` |
| Properties | PascalCase | `RazaoSocial`, `OmieId` |
| Private Fields | `_camelCase` | `_dbContext`, `_logger` |
| Methods | PascalCase | `ListarClientesAsync` |

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

- Use `public` schema by default
- Consider snake_case for column names (e.g., `razao_social`)
- Generate migrations from `Tabatine.Infrastructure`

---

## API Omie Integration

### Rate Limits (Mandatory)

- **Max requests**: 240/minute
- **Simultaneous**: max 4 requests
- **Block per ID**: 60s between calls to same ID
- **HTTP 425**: After 10 incorrect requests = 30min block

### Pagination

- Use `registros_por_pagina`: 50-100 (max 500)
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

- Use `ILogger<T>` injected via constructor
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
└── Tabatine.Worker/         # Background service, endpoints
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

## Existing Agent Rules

Refer to these files for additional context:

- `.agents/rules/geral.md` - Project overview
- `.agents/rules/dot-net-standards.md` - .NET conventions
- `.agents/rules/omie-validator.md` - Omie API rules
- `.agents/rules/supabase-db.md` - Database guidelines

---

## Key Patterns

1. **Incremental Sync**: Always use date filters to fetch only changes
2. **Idempotency**: Jobs must be restartable without duplicating data
3. **Distributed Lock**: Use `IDistributedLockService` for multi-container deployments
4. **Health Checks**: Endpoint `/health` monitors DB and Omie connectivity
