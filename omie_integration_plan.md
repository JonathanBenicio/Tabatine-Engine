# Plano de Integração Omie (.NET 10)

Esta documentação propõe uma arquitetura escalável e resiliente em **.NET 10** voltada para a sincronização de dados da API Omie para um banco de dados relacional ou NoSQL (como PostgreSQL ou SQL Server).

## 1. Arquitetura Geral do Worker

O sistema pode ser idealizado como um **Worker Service** em .NET (usando o `Microsoft.Extensions.Hosting`) que roda em background e orquestra _Jobs_ (ex.: usando Hangfire ou Quartz.NET) para diferentes módulos (Clientes, Produtos, Vendas, Notas Fiscais, etc.). 

**Camadas Principais:**
- **Scheduler (Agendador):** Controla o tempo de execução de cada job (ex: rodar a cada 5 minutos).
- **Extrator (API Client):** Camada de integração encapsulando chamadas HTTP para a API da Omie usando `HttpClientFactory` com `Polly` para tratamento de retry.
- **Processador / Transformador:** Transforma o JSON originado da Omie no modelo de domínio e verifica se precisa atualizar (Upsert) na base de dados (EF Core ou Dapper).
- **Repositório de Estado (Sync State):** Uma tabela/repositório que grava a data e hora do último processamento com sucesso de cada rotina (ex: `LastSyncDate`).

---

## 2. Tecnologias Recomendadas

- **Plataforma:** .NET 10 (Worker Service).
- **Client HTTP:** `IHttpClientFactory` nativo.
- **Resiliência e Rate Limits:** `.NET Resilience Extensions` (`Microsoft.Extensions.Http.Resilience`) para aplicar políticas nativas e avançadas de Retry, Timeout e Circuit Breaker.
- **Banco de Dados Relacional:** **Supabase (PostgreSQL)** acessado de forma robusta via **Entity Framework Core** (`Npgsql.EntityFrameworkCore.PostgreSQL`).
- **Banco de Controle e Cache:** **Redis**. Fundamental para controle distribuído de Rate Limits, coordenação entre múltiplas instâncias do worker (Distributed Locks) e caching rápido.

---

## 3. Estratégias de Implementação

### 3.1. Sincronização via Listagens Incrementais

Em cada iteração de um Job de entidade (ex: `SincronizarVendas`):

1. **Recupera o Estado Anterior:** Lê do banco de dados (ex. tabela `IntegrationSyncState`) o último filtro temporal rodado (ex: `01/03/2026 14:00:00`). Caso não exista contexto, buscar de um passado pré-determinado (Carga Inicial).
2. **Setup da Paginação:** Define página `1` inicial e limites recomendados por requisição, ex.: `registros_por_pagina = 100` (ou o ideal ajustado até 500, desde que não dê timeout).
3. **Loop de Páginas:** Ao realizar o `POST` para `ListarDocumentos` com o filtro:
   - Extrai o retorno (lista).
   - Inseri na base de dados local.
   - Avança para a página `2`.
   - Cessa a busca quando `numero_registros` da página atual for `< pagina_length` ou `total_de_paginas` atingido.
4. **Atualiza o Estado Anterior:** Em caso de o Job rodar com sucesso integral, salva o `LastSyncDate` final para que a próxima execução traga apenas a diferença (_delta_).

### 3.2. Gestão de Rate Limits e Concorrência

Para não ultrapassar as regras estritas da Omie, devemos moldar a camada de envio HTTP:

- **Controle Simultâneo:** Usar `SemaphoreSlim(4)` dentro dos repositórios ou uma limitação no HttpClient/Resilience Pipeline (`AddConcurrencyLimiter`) para impedir que a mesma chave (IP+Method+AppKey) passe de **4 envios paralelos**.
- **Rate Limit por Minuto (240 por IP+AppKey+Method):** 
  - Usar um `RateLimiter` (`System.Threading.RateLimiting.TokenBucketRateLimiter` ou `FixedWindowRateLimiter`).
  - Configure para permitir, por exemplo, de forma conservadora **200 requisições a cada 60 segundos** de recarga. O .NET colocará as outras threads em fila aguardando.

### 3.3. Tratamento de Erros e Bloqueios (Polly / Http Resilience)

Uma Pipeline de resiliência nativa do .NET pode mitigar o risco de bloqueio de 30 minutos (425) e gerenciar Backoffs:

* **Política de Retry com Exponential Backoff:**
  - Caso receba 429 (Too Many Requests) ou erros temporários de conexão (50x): Esperar um tempo e retentar. E.g.: Espera de `jitter` randomico entre `(2^retry) * 2` segundos (ex: 2s, 4s, 8s).
  - Limite de Retries para não entrar em looping na infra deles e evitar acumular as famosas "10 requisições erradas sequenciais". **Abortar e "lançar o throw" depois de 3 erros não mapeados.**
* **Circuit Breaker Avançado:** Configurar caso ocorram 5 falhas da API com código HTTP > 400 em sequência, que o "Circuito abra" (Circuit Breaker) e congele todo o serviço por **1 minuto**, impedindo chamadas (pois na 10ª ele poderia bloquear nossa App Key por 30m).
* **Bloqueio de 60s por ID:** Como a arquitetura focará em _listagem geral por data_, requerer registros por "ID avulso" será a exceção, só para necessidades interativas de tempo real com os usuários, escapando desta restrição.

---

## 4. Estrutura do Projeto (Esboço)

```bash
/Tabatine-Engine
  /src
    /Tabatine.Worker           # (Worker Service principal, hospeda IHostedService e Schedulers)
    /Tabatine.Omie.Client      # (Abstração da API, HttpClientFactory, DTOs Json)
    /Tabatine.Infrastructure   # (Entity Framework Models DB, Contexto local, Repositórios)
    /Tabatine.Core             # (Regras de Sincronização, Interfaces, Modelos de Entidades)
```

## Próximos Passos

1. Validar a modelagem de entidades essenciais (Quais categorias de tabelas vão migrar? Ex: Clientes, Produtos, Pedidos, Finanças).
2. Devo criar o Scaffold base deste worker na pasta `/Tabatine-Engine` usando a estrutura sugerida acima e os pacotes de `Http Resilience` do .NET?
