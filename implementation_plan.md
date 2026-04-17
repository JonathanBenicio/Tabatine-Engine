# Plano de Implementação - Estabilização de Webhooks e Sincronização

Este plano detalha as ações para estabilizar os testes de integração e endurecer os serviços de sincronização contra falhas de concorrência e problemas de infraestrutura.

## Mudanças Propostas

### Infraestrutura de Testes

#### [MODIFY] [IntegrationTestWebAppFactory.cs](file:///c:/Users/Jonathan/Documents/Developer/GitHub/Tabatine-Engine/src/Tabatine.Worker.IntegrationTests/IntegrationTestWebAppFactory.cs)
- [x] Corrigir caminho do `appsettings.Local.json` usando `ContentRootPath`.

#### [MODIFY] [SandboxIntegrationTestWebAppFactory.cs](file:///c:/Users/Jonathan/Documents/Developer/GitHub/Tabatine-Engine/src/Tabatine.Worker.IntegrationTests/SandboxIntegrationTestWebAppFactory.cs)
- [x] Refinar extração de `IConfiguration`.
- [ ] Validar carregamento de chaves Omie:Sandbox.

#### [MODIFY] [SandboxOmieHelper.cs](file:///c:/Users/Jonathan/Documents/Developer/GitHub/Tabatine-Engine/src/Tabatine.Worker.IntegrationTests/Helpers/SandboxOmieHelper.cs)
- [x] Adicionar headers `User-Agent` e `Accept`.
- [x] Adicionar captura de corpo de erro em falhas 500.
- [ ] Refinar payload `UpsertCliente` com campos obrigatórios (`tipo_pessoa`, `codigo_país`).
- [ ] Aplicar `JsonIgnoreCondition.WhenWritingNull`.

### Sincronização e Concorrência

#### [MODIFY] [NotaFiscalSyncService.cs](file:///c:/Users/Jonathan/Documents/Developer/GitHub/Tabatine-Engine/src/Tabatine.Infrastructure/Services/NotaFiscalSyncService.cs)
- [x] Implementar propagação de locks (`preAcquiredLocks`).
- [x] Corrigir mapeamento de produto para usar `CodigoProduto` (string).

#### [MODIFY] [PedidoSyncService.cs](file:///c:/Users/Jonathan/Documents/Developer/GitHub/Tabatine-Engine/src/Tabatine.Infrastructure/Services/PedidoSyncService.cs)
- [x] Refinar limpeza de coleções de itens.
- [x] Adicionar logs diagnósticos.

#### [MODIFICAR] [ProdutoSyncService.cs](file:///c:/Users/Jonathan/Documents/Developer/GitHub/Tabatine-Engine/src/Tabatine.Infrastructure/Services/ProdutoSyncService.cs)
- [ ] Implementar padrão de retry para `DbUpdateConcurrencyException`.

#### [MODIFICAR] [ClienteSyncService.cs](file:///c:/Users/Jonathan/Documents/Developer/GitHub/Tabatine-Engine/src/Tabatine.Infrastructure/Services/ClienteSyncService.cs)
- [ ] Implementar padrão de retry para `DbUpdateConcurrencyException`.

#### [MODIFICAR] [VendedorSyncService.cs](file:///c:/Users/Jonathan/Documents/Developer/GitHub/Tabatine-Engine/src/Tabatine.Infrastructure/Services/VendedorSyncService.cs)
- [ ] Implementar padrão de retry para `DbUpdateConcurrencyException`.

## Questões Abertas

> [!IMPORTANT]
> O Erro 500 no Sandbox persiste mesmo com chaves válidas. Suspeitamos de campos obrigatórios ausentes ou formatação do JSON.

---

## Plano de Verificação

### Testes Automatizados
- Executar `dotnet test --filter "Category=Sandbox"` para validar E2E.
- Executar `dotnet test --filter "Category=Integration"` para validar serviços em container.
- Filtros recomendados:
  - `dotnet test --filter "FullyQualifiedName~ProdutoWebhookIntegrationTests"`
  - `dotnet test --filter "FullyQualifiedName~ResilienciaInfrastructuraIntegrationTests"`
  - `dotnet test --filter "FullyQualifiedName~PedidoWebhookIntegrationTests"`

### Verificação Manual
- Auditar logs do Worker para confirmar o encerramento gracioso e sucesso nas atualizações de status.
