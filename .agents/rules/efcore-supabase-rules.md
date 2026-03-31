---
trigger: always_on
description: Regras estritas para modelagem de banco de dados usando EF Core e PostgreSQL (Supabase) no projeto Tabatine Engine.
---

# Banco de Dados e Supabase — Tabatine Engine

O projeto `Tabatine Engine` utiliza **Supabase (PostgreSQL)** como banco de dados principal. O agente DEVE seguir estas regras estritas para garantir compatibilidade, performance e organização do schema.

## 1. Nomenclatura (Snake_Case) e Schema

PostgreSQL é case-sensitive quando nomes são colocados entre aspas e a comunidade adota estritamente `snake_case`.
- **C# Domain:** As classes e propriedades C# continuam em `PascalCase`.
- **Database Schema:** O banco de dados DEVE usar `snake_case`.
- **Implementação:** O agente NÃO precisa (e não deve) usar anotações como `[Column("nome_coluna")]`. Confie no pacote `EFCore.NamingConventions` (via `.UseSnakeCaseNamingConvention()` no DbContext) para fazer a conversão automática.
- **Esquema**: Por padrão, use o esquema `public`.
- **Identificadores**: Use `Guid` para chaves primárias internas e `long` para `OmieId`.

---

## 2. Isolamento de Configuração (Fluent API)

**NUNCA** adicione configurações de mapeamento de entidades diretamente no método `OnModelCreating` do `AppDbContext` e **NUNCA** use Data Annotations (`[Table]`, `[Key]`) nas entidades de domínio.

- TODA entidade deve ter sua própria classe de configuração implementando `IEntityTypeConfiguration<T>`.
- As configurações devem ser salvas na pasta `Tabatine.Infrastructure/Data/Configurations/`.
- Tipos específicos do PostgreSQL, como `jsonb` ou `text[]`, devem ser mapeados utilizando a interface da configuração isolada.
- **Shadow Properties**: Evite usar shadow properties para campos críticos; prefira defini-los explicitamente nas entidades.

### Exemplo (The Golden Path)
```csharp
namespace Tabatine.Infrastructure.Data.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tabatine.Core.Entities;

public class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> builder)
    {
        // O nome da tabela será convertido para 'clientes' pelo NamingConvention,
        // mas explicitamos pluralização lógica se necessário.
        builder.ToTable("clientes");

        // Primary Key (Geralmente o código da Omie em formato BIGINT)
        builder.HasKey(c => c.Id);
        
        // Property configs
        builder.Property(c => c.RazaoSocial)
               .IsRequired()
               .HasMaxLength(255);
               
        // Controle de concorrência e auditoria
        builder.Property(c => c.CreatedAt)
               .HasDefaultValueSql("now()");
    }
}
```

---

## 3. Tipagem de Dados Específica do PostgreSQL

O agente deve estar ciente do mapeamento correto de tipos CLR para tipos PostgreSQL:

- **string longo ou indefinido**: Mapeie para `text` (Não limite a `varchar(MAX)`, no Postgres `text` é mais eficiente).
- **DateTime**: Use propriedades UTC (`DateTime.UtcNow`). O Postgres armazenará como `timestamp with time zone`.
- **Ids da Omie** (ex: `codigo_cliente_omie`): Como a Omie usa números inteiros muito grandes, mapeie sempre para `long` no C#, que vira `bigint` no PostgreSQL.
- **Campos monetários** (`decimal`): Especifique a precisão, ex: `builder.Property(p => p.Valor).HasPrecision(15, 2);`.

---

## 4. O DbContext

O `AppDbContext` deve permanecer limpo. Ele deve apenas expor os `DbSet<T>` e aplicar as configurações via reflection.

```csharp
// DO THIS: AppDbContext limpo
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    
    // Aplica todas as configurações da pasta 'Configurations' automaticamente
    modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
}
```

---

## 5. Row Level Security (RLS)

- Ao interagir diretamente com o Supabase via clientes web/mobile, o RLS é mandatório.
- Para o **Engine** (este projeto), as conexões usam geralmente a `SERVICE_ROLE` ou uma string de conexão administrativa, ignorando RLS.
- **Aviso**: Verifique se as alterações no esquema não quebram políticas de RLS existentes no Supabase.

---

## 6. Migração de Dados e Backup

- **Migrações**: Devem ser geradas com o projeto de infraestrutura (`Tabatine.Infrastructure`) e executadas apontando para a string de conexão do Supabase.
- Sempre teste migrações localmente antes de aplicar em produção.
- Use `LogEntry` para auditar alterações massivas ou erros críticos de integridade de dados.

---

## 7. Conectividade Crítica (Npgsql + Supavisor)

Ao usar o Pooler do Supabase (Supavisor) com .NET/Npgsql (especialmente em Windows), existem flags obrigatórias para evitar o crash `ObjectDisposedException` no handshake:

- **Flags Obrigatórias na Connection String**:
  - `Pooling=false`: O Pooler do Supabase já gerencia conexões; o pooling do lado do cliente causa conflitos de estado de sessão.
  - `No Reset On Close=true`: Essencial para evitar o crash no `ManualResetEventSlim.Reset()` ao fechar conexões.
  - `GssEncryptionMode=Disable`: Evita uma regressão de performance e handshake comum em proxies como o Supavisor.

- **Seleção de Host**:
  - **Direto (`db.[REF].supabase.co`)**: Geralmente é **apenas IPv6**. Se a rede local não suportar IPv6, a conexão falhará com erro de DNS.
  - **Pooler (`[REGION].pooler.supabase.com`)**: Oferece **IPv4**. É a escolha recomendada para ambientes locais com suporte IPv6 limitado, desde que as flags acima sejam usadas.