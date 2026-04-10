# 🧬 Mutation Testing com Stryker.NET

## O que é Mutation Testing?

Mutation Testing avalia a **qualidade dos seus testes**, não apenas a cobertura de linhas.

O Stryker modifica deliberadamente o código de produção (*mutações*) e verifica se os testes detectam essas mudanças:

| Conceito | Significado |
|----------|------------|
| **Mutante Morto** ✅ | O teste detectou a mutação → teste eficaz |
| **Mutante Sobrevivente** ❌ | Nenhum teste detectou → lacuna no teste |
| **Mutation Score** | `Mortos / Total × 100%` |

> **Cobertura de linha ≠ qualidade de teste.** Um teste pode cobrir uma linha sem fazer nenhuma assertion relevante.

---

## Configuração do Projeto

### Arquivo de Configuração

```json
// src/Tabatine.Infrastructure/stryker-config.json
{
  "stryker-config": {
    "project": "Tabatine.Infrastructure.csproj",
    "test-projects": ["../../Tabatine.Worker.IntegrationTests/..."],
    "mutation-level": "Standard",
    "thresholds": {
      "high": 75,   // Mutation Score acima disso → 🟢 verde
      "low": 50,    // Abaixo disso → 🟡 amarelo (warning)
      "break": 40   // Abaixo disso → 🔴 CI falha
    },
    "mutate": [
      "Services/**/*.cs",      // ✅ Incluído (lógica de sync)
      "Repositories/**/*.cs",  // ✅ Incluído (acesso a dados)
      "!Data/Configurations/**/*.cs", // ❌ Excluído (boilerplate EF)
      "!Data/Migrations/**/*.cs"      // ❌ Excluído (gerado automaticamente)
    ]
  }
}
```

### Tool Manifest

O Stryker está registrado em `.config/dotnet-tools.json` como **ferramenta local**:

```bash
# Instala a ferramenta localmente (necessário na 1ª execução por máquina)
dotnet tool restore

# Versão instalada: 4.14.0
```

---

## Como Executar

### Localmente (recomendado antes de PRs)

```bash
# Na raiz do repositório
dotnet tool restore

# Executar Stryker na camada Infrastructure
dotnet stryker \
  --project src/Tabatine.Infrastructure/Tabatine.Infrastructure.csproj \
  --config-file src/Tabatine.Infrastructure/stryker-config.json

# O relatório HTML é gerado em:
# src/Tabatine.Infrastructure/StrykerOutput/reports/mutation-report.html
```

### Via GitHub Actions (CI/CD)

O workflow `.github/workflows/mutation-tests.yml` executa automaticamente:

| Trigger | Quando |
|---------|--------|
| `workflow_dispatch` | Execução manual sob demanda |
| `schedule` | Todo dia 1 de cada mês, às 3h UTC |
| `pull_request → main` | PRs que alteram `Services/**` ou `Repositories/**` |

**Para executar manualmente:**
1. Acesse a aba **Actions** no GitHub
2. Selecione **Mutation Tests (Stryker.NET)**
3. Clique em **Run workflow**

---

## Interpretando o Relatório

### Scores de Referência

| Score | Status | Ação |
|-------|--------|------|
| ≥ 75% | 🟢 Alto | Excelente qualidade de testes |
| 50–74% | 🟡 Médio | Revisar mutantes sobreviventes |
| 40–49% | 🔴 Baixo | CI passa com warning crítico |
| < 40% | 💀 Crítico | **CI falha (break threshold)** |

### Tipos de Mutação (Standard Level)

| Mutação | Exemplo Original | Exemplo Mutado |
|---------|-----------------|----------------|
| Operador Aritmético | `valorPago + 1` | `valorPago - 1` |
| Operador Lógico | `saldo > 0` | `saldo >= 0` |
| Condicional | `if (a && b)` | `if (a \|\| b)` |
| Valor de Retorno | `return true` | `return false` |
| Negação | `!isAberto` | `isAberto` |

### Exemplo de Mutante Sobrevivente (ruim ❌)

```csharp
// Código Original
public bool IsQuitado() => ValorSaldo == 0m;

// Mutação gerada pelo Stryker:
public bool IsQuitado() => ValorSaldo != 0m;  // ← se nenhum teste captura isso, sobrevive!
```

**Fix:** Adicionar assertion direta para o caso `ValorSaldo == 0`:

```csharp
titulo.IsQuitado().Should().BeTrue();
titulo.ValorSaldo.Should().Be(0m);  // assertion explícita mata o mutante
```

---

## Estratégia de Evolução do Score

### Sprint Atual (Baseline)

Execute o Stryker localmente para obter o **score inicial** antes de estabelecer metas:

```bash
dotnet stryker --project src/Tabatine.Infrastructure/... --config-file ...
```

### Plano de Melhoria

| Sprint | Meta | Ação |
|--------|------|------|
| Baseline | Medir score atual | Executar Stryker, analisar relatório |
| Sprint 1 | Score ≥ 50% | Adicionar assertions específicas nos cenários de pagamento parcial |
| Sprint 2 | Score ≥ 60% | Cobrir branches dos `SyncService` com mutações críticas |
| Sprint 3 | Score ≥ 75% | Focar nos mutantes sobreviventes de lógica de negócio |

### Focando nos Mutantes Que Importam

Priorize mutantes sobreviventes em:
1. **Lógica de comparação financeira** (`ValorSaldo`, `ValorPago`, `StatusTitulo`)
2. **Condições de paginação** (`pagina <= totalPaginas`)
3. **Guards de upsert** (`if (entity == null)`)
4. **Decisões de retry/erro** (`catch` handlers)

---

## Gerando Relatório Estático para Revisão

```bash
# Gera relatório HTML abrível sem servidor
dotnet stryker --reporter html

# Abre o relatório no navegador padrão (Windows)
Start-Process src/Tabatine.Infrastructure/StrykerOutput/reports/mutation-report.html
```

---

## Links Úteis

- [Documentação Oficial Stryker.NET](https://stryker-mutator.io/docs/stryker-net/introduction/)
- [Tipos de Mutação Suportados](https://stryker-mutator.io/docs/stryker-net/mutations/)
- [Configuração Avançada](https://stryker-mutator.io/docs/stryker-net/configuration/)
