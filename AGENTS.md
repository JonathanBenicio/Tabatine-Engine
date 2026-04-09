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

# Run single test (when tests exist)
dotnet test --filter "FullyQualifiedName~TestClassName.MethodName"
```

> **Note**: This project currently has no test suite. Agents should create tests when implementing new features.

---

## Code Style Guidelines

### Naming Conventions

| Element | Convention | Example |
|---------|-----------|---------|
| Methods | PascalCase | `ListarClientesAsync` |
| Properties | PascalCase | `RazaoSocial` |
| Private Fields | camelCase + underscore | `_omieClient` |
| Interfaces | PascalCase + I prefix | `ISyncService` |
| Classes | PascalCase | `ClienteSyncService` |

### Language Rules

- **Domain (Entities)**: Use Portuguese (ERP domain terms)
  - `Cliente`, `PedidoVenda`, `ItemPedido`
- **Architecture/Infrastructure**: Use English
  - `SyncService`, `DbContext`, `Repository`, `Worker`

### Global Usings

Each project has a `GlobalUsings.cs` in its root. Common namespaces:
- `Tabatine.Core.Entities`
- `Tabatine.Infrastructure.Data`
- `Tabatine.Infrastructure.Services`
- `Tabatine.Omie.Client.Models`
- `Microsoft.EntityFrameworkCore`

### File-Scoped Namespaces

Use file-scoped namespaces in all files. Follow pattern `Tabatine.<Project>.<Folder>`:
```csharp
namespace Tabatine.Infrastructure.Services;
```

### Comments

- **Comments**: Write in Portuguese for business logic, English for infrastructure
- **NO comments** on trivial code or obvious implementations
- Use comments to explain *why*, not *what*

---

## C# 10+ Patterns

### Primary Constructors

Use Primary Constructors for dependency injection. Avoid explicit private fields:
```csharp
// CORRETO
public class ClienteSyncService(
    IOmieClient omieClient,
    ILogger<ClienteSyncService> logger) : ISyncService
{
    public async Task SyncAsync(CancellationToken ct)
    {
        logger.LogInformation("Sync started...");
    }
}

// EVITAR
public class ClienteSyncService
{
    private readonly IOmieClient _omieClient;
    public ClienteSyncService(IOmieClient omieClient) => _omieClient = omieClient;
}
```

### DTOs (Records)

DTOs in `Tabatine.Omie.Client.Models` must use `record` with `{ get; init; }`:
```csharp
public record ClienteDto
{
    [JsonPropertyName("codigo_cliente_omie")]
    public long CodigoClienteOmie { get; init; }
    
    [JsonPropertyName("razao_social")]
    public string RazaoSocial { get; init; } = string.Empty;
}
```

### Memory Safety (Antigravity Rule)

**NEVER** return `List<T>` in paginated methods. Use `IAsyncEnumerable<T>`:
```csharp
public async IAsyncEnumerable<Cliente> FetchClientesAsync(
    [EnumeratorCancellation] CancellationToken ct)
{
    int page = 1;
    while (true)
    {
        var response = await _client.ListarClientesAsync(page, ct);
        if (response.Clientes.Count == 0) break;
        
        foreach (var dto in response.Clientes)
            yield return MapToDomain(dto);
        page++;
    }
}
```

---

## Entity Framework Core

### Base Entity

All Omie entities inherit from `OmieEntityBase`:
```csharp
public abstract class OmieEntityBase
{
    public Guid Id { get; set; }           // Internal UUID
    public long OmieId { get; set; }       // Omie numeric ID
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? OmieUpdatedAt { get; set; }
}
```

### Configuration Files

Each entity has a dedicated configuration class in `Data/Configurations/`:
```csharp
public class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> builder)
    {
        builder.ToTable("clientes");
        builder.HasKey(c => c.Id);
        builder.HasIndex(c => c.OmieId).IsUnique();
        builder.Property(c => c.RazaoSocial).HasMaxLength(255).IsRequired();
    }
}
```

**Rules**:
- **NO** `[Column]` or `[Table]` attributes (use Fluent API)
- **NO** adding config in `OnModelCreating` directly
- Use `modelBuilder.ApplyConfigurationsFromAssembly()` in DbContext
- Database uses **snake_case** automatically via `UseSnakeCaseNamingConvention()`

### Connection String (Supabase/Supavisor)

Mandatory flags to prevent handshake crashes:
- `Pooling=false`
- `No Reset On Close=true`
- `GssEncryptionMode=Disable`

---

## Error Handling

### Result Pattern (Mandatory)

**NEVER** throw exceptions for business flow control. Use `Result<T>`:
```csharp
public async Task<Result<Cliente>> GetClienteAsync(long omieId, CancellationToken ct)
{
    var cliente = await _dbContext.Clientes.FirstOrDefaultAsync(c => c.OmieId == omieId, ct);
    if (cliente == null)
        return Result<Cliente>.Failure("Cliente não encontrado");
    return Result<Cliente>.Success(cliente);
}
```

### When to Use try/catch

Reserve for critical infrastructure failures only (network, DB connection).

### Logging

| Level | Usage |
|-------|-------|
| `Information` | Process milestones, sync page X |
| `Warning` | Recoverable errors, inconsistencies |
| `Error` | Critical failures stopping sync |

---

## Dependency Injection

| Service Type | Lifetime |
|--------------|----------|
| DbContext-dependent | `AddScoped` |
| API Clients (HttpClient) | `AddSingleton` |
| Configuration | `IOptions<T>` |

---

## Omie API Integration

### Rate Limits
- **Max requests**: 240/minute
- **Simultaneous**: max 4 requests
- **Block per ID**: 60s between calls to same ID
- **HTTP 425**: After 10 incorrect requests = 30min block

### Pagination
- Use `registros_por_pagina`: max **100**
- Handle empty responses (codes `Client-5113`, `Client-101`)

### Sync Pattern
1. Get last sync date from `ISyncStateRepository`
2. Fetch paginated data from Omie
3. Upsert (check `OmieId` exists before insert)
4. Compare `OmieUpdatedAt` to avoid redundant updates
5. Save sync cursor

---

## Project Structure

```
src/
├── Tabatine.Core/           # Domain entities, interfaces
│   ├── Entities/            # POCO classes (Portuguese names)
│   └── Interfaces/          # ISyncService, IOmieClient
├── Tabatine.Infrastructure/ # EF Core, sync services
│   ├── Data/Configurations/ # IEntityTypeConfiguration classes
│   ├── Repositories/        # Data access
│   └── Services/            # Sync services (Portuguese entity names)
├── Tabatine.Omie.Client/    # Omie API client
│   └── Models/             # DTOs (records, Portuguese names)
├── Tabatine.Worker/         # Background service, endpoints
│   ├── Endpoints/           # Minimal API endpoints
│   ├── Extensions/          # DI registration
│   └── Services/           # Background workers
└── Tabatine.ConnectionTester/
```

---

## Key Patterns

1. **Idempotency**: Jobs must be restartable without duplicating data
2. **Distributed Lock**: Use `IDistributedLockService` for multi-container
3. **Health Checks**: Endpoint `/health` monitors DB and Omie connectivity

---

## Existing Agent Rules

Refer to these files for detailed architecture rules:

- `.agents/rules/dot-net-standards.md` - C# 10 standards & Primary Constructors
- `.agents/rules/efcore-supabase-rules.md` - Database & EF Core standards
- `.agents/rules/omie-api-rules.md` - Omie API & Memory Safety
- `.agents/rules/custom_instructions.md` - Language and communication rules

### Specialized Skills

- `.agents/skills/omie-api-skills/SKILL.md` - Omie API integration
- `.agents/skills/omie-webhooks/SKILL.md` - Omie Webhooks
- `.agents/skills/supabase-postgres-best-practices/SKILL.md` - Postgres optimization
