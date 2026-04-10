# ADR-003: Estratégia de Qualidade e Governança de Testes

## Status
Accepted

## Contexto
O Tabatine Engine é um middleware crítico entre o Omie ERP e o banco de dados local. Falhas na sincronização podem causar inconsistências financeiras graves. Até o momento, o projeto evoluiu com testes de integração funcionais, mas sem métricas de cobertura ou garantias de eficácia (mutação).

## Decisão
Adotaremos uma abordagem de "Cobertura Progressiva e Multicamada":

1.  **Medição Técnica**: Uso de `Coverlet` + `ReportGenerator` para visibilidade de linhas/galhos de código.
2.  **Threshold Dinâmico**: Iniciamos com 50% de cobertura mínima, incrementando 5% a cada ciclo (sprint/semana) até atingirmos 80%+.
3.  **Auditoria de Negócio**: A cobertura não será apenas por linha, mas por "Entidade Omie". Usaremos um GitHub Project para mapear quais entidades do domínio possuem testes de integração completos.
4.  **Testes de Mutação**: Introdução do `Stryker.NET` para garantir que nossos testes são capazes de detectar mudanças sutis na lógica.

## Consequências
### Positivas
- Maior confiança em refactorings.
- Prevenção de regressões em sincronizações complexas (ex: Clientes, Vendas).
- Visibilidade clara da saúde do projeto para stakeholders.

### Negativas/Trade-offs
- Aumento do tempo de execução do pipeline (especialmente com Mutação).
- Necessidade de manutenção constante das sementes de teste (Testcontainers).

## Referências
- [Issue #24: Automação de Cobertura](https://github.com/JonathanBenicio/Tabatine-Engine/issues/24)
- [Issue #25: Auditoria de Domínio](https://github.com/JonathanBenicio/Tabatine-Engine/issues/25)
- [Issue #26: Testes de Mutação](https://github.com/JonathanBenicio/Tabatine-Engine/issues/26)
