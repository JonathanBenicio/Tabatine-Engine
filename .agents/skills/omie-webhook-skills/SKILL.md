---
description: Regras e estrutura de Ingestion, Background Processing e filas assíncronas projetadas para processar notificações push em tempo real emitidas pelo ERP Omie.
---

# Skills: Componentes do Webhook Omie

[cite_start]Para implementar o ecossistema de webhooks da Omie respeitando suas características[cite: 6], o sistema deve possuir as seguintes *Skills* (módulos/capacidades):

## 1. Omie Webhook Receiver (Ingestion Skill)
- **Função:** Endpoint HTTP de alta performance.
- **Capacidades:**
  - Deserializar payloads para as classes base (ex: `OmieWebhookRequest`).
  - Injetar o serviço de Fila/Armazenamento.
  - [cite_start]Retornar incondicionalmente HTTP 200 (se o enfileiramento foi bem sucedido), bloqueando qualquer processamento denso[cite: 17, 30].

## 2. Event Queue Manager (Broker Skill)
- **Função:** Gerenciar o armazenamento temporário dos eventos.
- **Capacidades:**
  - [cite_start]Armazenar o evento completo (armazenamento de dados para processamento posterior)[cite: 30, 31].
  - Prover controle de concorrência e idempotência (verificar se a mensagem com o mesmo ID já foi processada recentemente).

## 3. Background Event Processor (Worker Skill)
- **Função:** Consumidor assíncrono que executa o "trabalho pesado".
- **Capacidades:**
  - Consumir filas continuamente (Worker Service).
  - Possuir injeção de dependência via *Scopes* para instanciar serviços transientes/scoped (como Entity Framework DbContext, `PedidoSyncService`, `NotaFiscalSyncService`).
  - Lidar de forma resiliente com falhas transientes de rede ao se comunicar com integrações externas (Notifications).

## 4. Dead-Letter & Error Logging (Observability Skill)
- **Função:** Garantir a visibilidade do processamento que roda em background.
- **Capacidades:**
  - Registrar com clareza o motivo de falha de uma sincronização no background.
  - [cite_start]Implementar uma política de repetição de falhas interna (já que a responsabilidade de tentar de novo agora é local, não da Omie)[cite: 31].

## 5. Referências e Implementação

Para ver o detalhamento técnico de como estas *Skills* foram implementadas na prática no ambiente do Tabatine (Engine), acesse a documentação detalhada:

- [Documentação da Arquitetura de Webhooks](references/webhook-docs.md)