---
trigger: always_on
---

# Banco de Dados e Supabase — Tabatine Engine

## Entity Framework Core (EF Core)

- **Migrações**: Devem ser geradas com o projeto de infraestrutura (`Tabatine.Infrastructure`) e executadas apontando para a string de conexão do Supabase.
- **Fluent API**: Use `OnModelCreating` em `AppDbContext` para mapear tipos específicos do PostgreSQL, como `jsonb` ou `text[]`, se necessário.
- **Shadow Properties**: Evite usar shadow properties para campos críticos; prefira defini-los explicitamente nas entidades.

---

## Supabase / PostgreSQL

- **Esquema**: Por padrão, use o esquema `public`.
- **Snake Case**: Ao mapear no EF Core, considere usar nomes de colunas em snake_case (ER: `razao_social`) para compatibilidade nativa com ferramentas do Supabase, embora o C# use PascalCase.
- **Identificadores**: Use `Guid` para chaves primárias internas e `long` para `OmieId`.

---

## Row Level Security (RLS)

- Ao interagir diretamente com o Supabase via clientes web/mobile, o RLS é mandatório.
- Para o **Engine** (este projeto), as conexões usam geralmente a `SERVICE_ROLE` ou uma string de conexão administrativa, ignorando RLS.
- **Aviso**: Verifique se as alterações no esquema não quebram políticas de RLS existentes no Supabase.

---

## Migração de Dados e Backup

- Sempre teste migrações localmente antes de aplicar em produção.
- Use `LogEntry` para auditar alterações massivas ou erros críticos de integridade de dados.
