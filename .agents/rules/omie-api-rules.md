---
trigger: model_decision
description: Regras de integração e arquitetura estritas para consumo da API Omie (Tabatine Engine).
---

# Regras de Arquitetura e Integração: APIs Omie

Essas regras definem os limites e restrições de alto nível para a integração. Para detalhes técnicos de *como* implementar (DTOs, Paginação, Result Pattern), consulte a skill `omie-api-integration-skills`.

## 1. Paginação Estrita (Antigravity Rule de Memória)
- **Constraint:** Todas as requisições de listagem (ex: `ListarClientes`, `ListarPedidos`) DEVEM definir o parâmetro `registros_por_pagina` com o valor máximo de **100**.
- **Constraint Antigravity:** É **estritamente proibido** retornar `List<T>` ou alocar todos os registros na memória em métodos que buscam dados massivos da Omie. O agente DEVE utilizar fluxos assíncronos (`IAsyncEnumerable<T>` com `yield return`).
- **Motivo:** Evitar alocações no LOH (Large Object Heap) e garantir escalabilidade do `Tabatine.Worker`.

## 2. Imutabilidade e Mapeamento de Modelos
- **Constraint Antigravity:** Qualquer classe na integração de API (Request/Response) DEVE ser implementada como um `record` imutável.
- **Padrão:** O mapeamento de campos com `[JsonPropertyName("nome_campo_omie")]` é obrigatório para compatibilidade semântica com o RPC da Omie.

## 3. Gestão de Erros e Fluxo (Result Pattern)
- **Constraint Antigravity:** NUNCA utilize controle de fluxo de erros lançando exceções (`throw new Exception()`). Exceptions no .NET devem ser minimizadas para alta performance.
- **Padrão:** Utilize o `Result Pattern` encapsulando falhas normais da Omie.

## 4. Consultas Incrementais e Otimização
- **Rule:** Nunca realize um "Full Sync" (download completo) em rotinas recorrentes.
- **Constraint:** Utilize obrigatoriamente filtros da API como `filtrar_por_data_de` e `filtrar_por_hora_de`. Adote a política de Upsert (verifica existência pelo campo `OmieId`) no banco de dados local.

## 5. Circuit Breaker e Respeito aos Rate Limits
- **Constraint:** A Omie aplica limites estritos de requisições por IP/App Key (ex: 240/min).
- **Padrão:** Qualquer código de integração DEVE prever a implementação de políticas de resiliência via Polly (Exponential Backoff). Consulte scripts de conectividade em `omie-api-skills` para validação de status.

## 6. Garantia de Documento de Origem
- **Regra de Negócio (Omie):** É proibido inserir lançamentos financeiros avulsos originados em pedidos comerciais. A aplicação externa se limitará a integrar e confirmar o **Documento de Origem**.

---
*Para padrões de código e templates de implementação, acesse:*
- [Omie API Core Skills](../skills/omie-api-skills/SKILL.md)
- [Omie Webhook Skills](../skills/omie-webhooks/SKILL.md)