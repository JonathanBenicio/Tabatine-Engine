---
name: Omie API Core Modules
description: Habilidades fundamentais e padrões de projeto (Resiliência, Pagination, Translators) exigidos para consumir a API REST financeira da Omie de forma escalável.
---

# Skills: Módulos de Integração com a API Omie

Para consumir a API de forma profissional e escalável, a arquitetura deve ser equipada com as seguintes *Skills*:

## 1. Omie HttpClient (Core Skill)
- **Função:** Cliente HTTP abstrato para encapsular a complexidade da API.
- **Capacidades:**
  - Gerir automaticamente as credenciais (`app_key` e `app_secret`) em todos os payloads.
  - Implementar um mecanismo de chamadas assíncronas (`async/await`) no .NET.
  - Formatar adequadamente o Content-Type como `application/json`.

## 2. Pagination Builder (List Skill)
- **Função:** Gerar e controlar os cursores/páginas nas listagens.
- **Capacidades:**
  - Abstrair a paginação: permitir que o programador consuma a lista inteira (ex: `IAsyncEnumerable` em .NET) enquanto a Skill resolve nos bastidores as requisições em blocos de 100 registos.
  - Gerir os parâmetros granulares de filtros temporais (`filtrar_apenas_alteracao`, etc.).

## 3. Rate Limiter & Retry Policy (Resilience Skill)
- **Função:** Proteger a aplicação contra quedas e punições da API.
- **Capacidades:**
  - Utilizar bibliotecas como **Polly** (no ecossistema .NET) para encapsular chamadas HTTP.
  - **Tratamento de Transient Faults:** Detetar exceções de rede ou o erro interno `PROTO_BYEBYE` da Omie.
  - **Exponential Backoff:** Tentar de novo em falhas (ex: após 2s, depois 4s, depois 8s) antes de declarar a requisição como morta.

## 4. Error Translator (Observability Skill)
- **Função:** Tornar os erros da API legíveis para o negócio.
- **Capacidades:**
  - Traduzir a estrutura de erro da Omie (que devolve um JSON contendo o código da falha e a causa, ex: "tag: [xxxx] não cadastrada") para exceções fortemente tipadas ou resultados legíveis no painel de controlo.