# Boas Práticas com DbContext no Tabatine-Engine

Este documento consolida as melhores práticas para o uso de `DbContext` (através do `AppDbContext`) no nosso ecossistema. Ele é um guia prático baseado na documentação oficial do Entity Framework Core, adaptado para nossa arquitetura.

## TL;DR - As Regras de Ouro

1.  **Use Pooling via Factory**: Nossa configuração padrão usa `AddPooledDbContextFactory` para máxima performance. Isso reutiliza instâncias do `DbContext`, evitando o custo de setup a cada operação.
2.  **Injete `IDbContextFactory` em Serviços Singleton**: Serviços como `DbDistributedLockService` **devem** injetar `IDbContextFactory<AppDbContext>` e criar um contexto por operação para garantir isolamento e thread-safety.
3.  **Injete `AppDbContext` em Serviços Scoped**: Para Webhooks, Endpoints e outros serviços com ciclo de vida `Scoped`, injete o `AppDbContext` diretamente. O container de DI cuidará de seu ciclo de vida.
4.  **NÃO é Thread-Safe**: Nunca, em hipótese alguma, compartilhe a mesma instância de `DbContext` entre múltiplas threads ou `Task`s concorrentes.
5.  **Mantenha o Ciclo de Vida Curto**: Sempre que criar um contexto manualmente via factory, use `await using var db = ...` para garantir que ele seja descartado (ou retornado ao pool) o mais rápido possível.

---

## 1. Configuração Padrão (O Padrão Tabatine)

Para garantir performance e consistência, centralizamos toda a configuração do `DbContext` no `ServiceCollectionExtensions`. O método padrão é `AddPooledDbContextFactory`.

```csharp
// Em: src/Tabatine.Worker/Extensions/ServiceCollectionExtensions.cs

public static IServiceCollection AddOmieInfrastructure(this IServiceCollection services, IConfiguration configuration)
{
    // ...
    services.AddPooledDbContextFactory<AppDbContext>(options =>
        options.UseNpgsql(
            connectionString,
            b => b.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)
                    .EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null)
                    // ... outras opções
        )
        .UseSnakeCaseNamingConvention()
    );

    // Registro do DbContext Scoped a partir da Factory
    services.AddScoped(sp => sp.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext());

    // ...
}
```

### Por que `AddPooledDbContextFactory`?

- **Performance**: Reutiliza instâncias do `DbContext`, reduzindo drasticamente a alocação de memória e o custo de inicialização de serviços internos. É ideal para aplicações de alta concorrência como o nosso Worker.
- **Flexibilidade**: Nos permite ter uma fonte única de configuração (`DbContextOptions`) e resolver o `DbContext` tanto em escopos `Singleton` quanto `Scoped` de forma segura.

---

## 2. Padrões de Uso

### Uso em Serviços Singleton (Ex: Background Jobs, Serviços de Lock)

Serviços com ciclo de vida `Singleton` não podem injetar dependências `Scoped` (como o `AppDbContext` direto). O padrão correto e obrigatório é injetar a factory e criar um contexto por método/operação.

**Exemplo Correto (`DbDistributedLockService`):**

```csharp
// Injeção via construtor
public class DbDistributedLockService(IDbContextFactory<AppDbContext> dbFactory) : IDistributedLockService
{
    public async Task<bool> TryAcquireLockAsync(...)
    {
        // Criação de um contexto de curta duração
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        // Lógica de banco de dados...
        
        // 'db' é descartado (retorna ao pool) automaticamente aqui.
    }
}
```

### Uso em Serviços Scoped (Ex: Webhook Handlers)

Serviços que rodam dentro de um escopo definido (como um request HTTP ou uma execução de job) podem injetar o `AppDbContext` diretamente.

```csharp
// Injeção via construtor
public class PedidoWebhookHandler(AppDbContext dbContext, ...) : IWebhookEventHandler
{
    public async Task HandleEventAsync(WebhookPayload payload)
    {
        // Usa o dbContext diretamente.
        var pedido = await dbContext.Pedidos.FindAsync(payload.Id);
        // ...
        await dbContext.SaveChangesAsync();
    }
}
```

O container de DI garante que a mesma instância de `DbContext` seja compartilhada dentro do mesmo escopo e descartada ao final.

---

## 3. Armadilhas Comuns a Evitar

### ⚠️ Concorrência na Mesma Instância

O `DbContext` não é thread-safe. Tentar executar múltiplas operações em paralelo na **mesma instância** resultará em uma `InvalidOperationException`.

**Exemplo ERRADO:**
```csharp
// Tenta buscar clientes e produtos em paralelo NO MESMO CONTEXTO
var clientesTask = dbContext.Clientes.ToListAsync();
var produtosTask = dbContext.Produtos.ToListAsync();

// ISSO VAI LANÇAR UMA EXCEÇÃO!
await Task.WhenAll(clientesTask, produtosTask);
```

**Solução CORRETA (usando a factory para criar contextos diferentes):**
```csharp
var clientesTask = Task.Run(async () => 
{
    await using var db = await dbFactory.CreateDbContextAsync();
    return await db.Clientes.ToListAsync();
});

var produtosTask = Task.Run(async () => 
{
    await using var db = await dbFactory.CreateDbContextAsync();
    return await db.Produtos.ToListAsync();
});

var results = await Task.WhenAll(clientesTask, produtosTask);
```

### ⚠️ Queries com Constantes (Poluição de Cache)

O EF Core faz cache do SQL gerado a partir da "forma" da sua query LINQ. Se você construir queries usando constantes, cada query terá uma "forma" diferente, impedindo o reuso do cache.

**Exemplo RUIM (Não faça isso):**
```csharp
// Cada valor de 'key' gera um novo plano de query no cache do EF
var query = db.SyncLocks.Where(l => l.LockKey == "some-key-1");
var query2 = db.SyncLocks.Where(l => l.LockKey == "some-key-2");
```

**Exemplo BOM (O padrão que já usamos):**
```csharp
string key = "some-key-1";
// A variável 'key' é transformada em um parâmetro SQL (@p0)
// A "forma" da query é sempre a mesma, permitindo reuso do cache.
var query = db.SyncLocks.Where(l => l.LockKey == key); 
```
Este padrão já é seguido corretamente em nosso código, como no `DbDistributedLockService`.
