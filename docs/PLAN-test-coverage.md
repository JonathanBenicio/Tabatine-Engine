# PLAN-test-coverage.md: Roteiro de Governança de Testes

## Contexto e Objetivo
Implementar uma infraestrutura de medição e melhoria contínua de testes para o Tabatine Engine, garantindo que a sincronização com o Omie ERP seja resiliente e verificável.

## 🛠️ Fases do Projeto

### Fase 1: Alicerce de Medição (A)
- [ ] **A.1**: Criar diretório `docs/adr` e inicializar `ADR-003-testing-strategy.md`.
- [ ] **A.2**: Configurar `dotnet test` para gerar relatórios de cobertura em formato Cobertura XML.
- [ ] **A.3**: Atualizar `.github/workflows/main_tabatine-worker.yml` para rodar o `ReportGenerator` e expor artefatos HTML.
- [ ] **A.4**: Adicionar verificação de Threshold (50%) no workflow de CI para falhar Pull Requests.

### Fase 2: Mapeamento de Domínio (B)
- [ ] **B.1**: Criar GitHub Project "Tabatine Quality Board".
- [ ] **B.2**: Popular o projeto com cards representando as entidades em `Tabatine.Core.Entities`.
- [ ] **B.3**: Iniciar auditoria manual via cards para identificar lacunas de sincronização.

### Fase 3: Refinamento de Qualidade (C)
- [ ] **C.1**: Configurar Stryker.NET no projeto de testes.
- [ ] **C.2**: Validar o "Mutation Score" inicial e documentar como rodá-lo localmente.

## 👥 Atribuições Sugeridas
- **Backend Specialist**: Configuração de Coverlet, Stryker.NET e CI/CD.
- **Orchestrator**: Criação da documentação (ADR) e gestão do GitHub Project.

## ✅ Checklist de Verificação (Phase V)
- [ ] Executar `dotnet test` e verificar se a pasta `coverage-results` é criada.
- [ ] Validar se o GitHub Actions exibe o sumário da cobertura no log.
- [ ] Validar se um PR com cobertura intencionalmente baixa (< 50%) é bloqueado.

---
[OK] Plano criado: docs/PLAN-test-coverage.md
