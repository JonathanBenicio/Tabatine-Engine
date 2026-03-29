---
description: Como adicionar uma nova entidade de sincronização da Omie
---

Siga estes passos para integrar uma nova entidade (ex: `Vendedor`, `Produto`, `ContaCorrente`) do Omie ERP para o Tabatine Engine.

### 1. Criar Modelos da Omie
No projeto `Tabatine.Omie.Client`, crie os DTOs de Request e Response baseados na documentação da Omie.
- Local: `src/Tabatine.Omie.Client/Models/NomeDaEntidade/`
- Arquivo: `ListarNomeDaEntidadeModels.cs`

### 2. Adicionar Método ao Client
Atualize a interface `IOmieClient` e sua implementação `OmieClient`.
- Arquivo: `IOmieClient.cs` e `OmieClient.cs`
- Adicione o método `Listar<Entidade>Async`.

### 3. Criar a Entidade de Domínio
No projeto `Tabatine.Core`, crie a entidade herdando de `OmieEntityBase`.
- Local: `src/Tabatine.Core/Entities/`
- Arquivo: `NomeDaEntidade.cs`

### 4. Atualizar o DbContext
No projeto `Tabatine.Infrastructure`, adicione o `DbSet` da nova entidade.
- Arquivo: `src/Tabatine.Infrastructure/Data/AppDbContext.cs`

### 5. Criar o Serviço de Sincronização
No projeto `Tabatine.Infrastructure`, crie uma nova classe que implemente `ISyncService`.
- Local: `src/Tabatine.Infrastructure/Services/`
- Arquivo: `NomeDaEntidadeSyncService.cs`
- **Dica**: Use o `ClienteSyncService.cs` como template para a lógica de "Upsert" e paginação.

### 6. Registrar a Dependência
No projeto `Tabatine.Worker`, registre o novo serviço no contêiner de DI.
- Arquivo: `src/Tabatine.Worker/Program.cs`
- Exemplo: `builder.Services.AddScoped<ISyncService, NomeDaEntidadeSyncService>();`

### 7. Criar e Aplicar Migração
Siga o workflow de [migração](migration.md) para gerar a tabela no Supabase.
