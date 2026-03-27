---
trigger: always_on
---

# Regras de Arquitetura: Webhooks Omie

## 1. Processamento Assíncrono Obrigatório (Fast Acknowledge)
- [cite_start]**Regra:** O endpoint de recebimento do Webhook DEVE retornar uma resposta HTTP 2XX o mais rápido possível[cite: 29, 30].
- [cite_start]**Motivo:** O timeout da Fila Principal da Omie é de apenas **7 segundos**[cite: 22, 23]. O processamento síncrono de regras de negócio (banco de dados, integrações externas, envio de notificações) viola essa regra e causará interrupção por timeout.
- [cite_start]**Padrão:** Receber -> Validar estrutura básica -> Enfileirar (RabbitMQ, SQS, Banco de Dados, ou Channel em memória) -> Retornar HTTP 200 OK[cite: 30, 31].

## 2. Prevenção de Bloqueio de Fila (FIFO Strict)
- **Regra:** Falhas no processamento da regra de negócio NÃO DEVEM ser repassadas para a Omie como erros HTTP (Ex: 500 Internal Server Error) se o payload já foi salvo com sucesso localmente.
- [cite_start]**Motivo:** A Omie processa os eventos sequencialmente (First In, First Out)[cite: 14, 15]. [cite_start]Se um endpoint não retornar 2XX, a solicitação será refeita e **nenhum outro evento subsequente do grupo será tentado** até que o primeiro obtenha sucesso[cite: 18, 26, 27].
- [cite_start]**Padrão:** O gerenciamento de falhas e as retentativas (Retry Pattern/Dead Letter Queue local) devem ser de responsabilidade exclusiva do sistema interno (Worker em background), liberando a fila da Omie[cite: 31].

## 3. Código de Resposta Restrito
- [cite_start]**Regra:** Retorne `HTTP 200 OK` exclusivamente quando o sistema tiver garantido a persistência da mensagem para processamento posterior[cite: 17, 31].
- [cite_start]**Motivo:** O retorno 2XX indica para a Omie que o evento foi processado e pode ser removido da fila[cite: 17]. Se retornar 200 sem ter salvado os dados, o evento será perdido irreversivelmente.

## 4. Idempotência
- **Regra:** O processamento em background deve ser idempotente.
- [cite_start]**Motivo:** Em caso de instabilidade de rede onde a Omie não receba o `200 OK` a tempo, ela reenviará o mesmo payload na Fila Principal (até 3 tentativas) ou na Dead Letter Queue (DLQ) da Omie (tentativas a cada 10 minutos por até 5 dias)[cite: 21, 23, 24, 25]. O sistema deve ignorar eventos duplicados.