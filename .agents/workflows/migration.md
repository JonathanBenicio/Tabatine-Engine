---
description: Como criar e aplicar migrações do EF Core no Supabase
---

Siga estes passos para atualizar o esquema do banco de dados no Supabase.

### 1. Criar a Migração
No terminal, execute o comando a partir da pasta raiz do projeto.
- Substitua `NomeDaMigracao` por um nome descritivo (ex: `AddVendedoresTable`).

```powershell
dotnet ef migrations add NomeDaMigracao --project src/Tabatine.Infrastructure --startup-project src/Tabatine.Worker --context AppDbContext
```

### 2. Revisar o Código Gerado
Verifique os arquivos criados na pasta `src/Tabatine.Infrastructure/Migrations/`.
- Verifique se os nomes das colunas e tipos de dados estão corretos (ex: `long` para IDs Omie, `jsonb` se necessário).

### 3. Aplicar a Migração
Aplique as alterações diretamente no Supabase.
- **Atenção**: Certifique-se de que a string de conexão no arquivo `.env.local` (na raiz da pasta `src/`) está apontando para o banco de dados correto do Supabase.

```powershell
dotnet ef database update --project src/Tabatine.Infrastructure --startup-project src/Tabatine.Worker
```

### 4. Verificar no Supabase
Acesse o painel do Supabase e verifique se as tabelas e colunas foram criadas conforme o esperado.
- Se houver políticas de **RLS** configuradas, verifique se elas precisam ser ajustadas para a nova tabela.

---

### Dicas Utéis
- **Remover última migração** (se ainda não aplicada): `dotnet ef migrations remove --project src/Tabatine.Infrastructure --startup-project src/Tabatine.Worker`
- **Script SQL Offline**: Se precisar do script SQL para rodar manualmente: `dotnet ef migrations script --project src/Tabatine.Infrastructure --startup-project src/Tabatine.Worker`
