# Regras de Arquitetura e Integração: APIs Omie

## 1. Paginação Estrita (Limite de Registos)
- **Regra:** Todas as requisições de listagem (ex: `ListarClientes`, `ListarPedidos`) DEVEM definir o parâmetro `registros_por_pagina` com o valor máximo de **100**.
- **Motivo:** A Omie bloqueia nativamente paginações superiores a 100 registos por página para garantir a máxima performance e estabilidade do sistema.
- **Padrão:** Implementar loops de paginação verificando a propriedade `total_de_paginas` ou `total_de_registros` retornada no cabeçalho da resposta.

## 2. Consultas Incrementais (Filtros de Data/Hora)
- **Regra:** Nunca realizar o download completo (Full Sync) de tabelas em operações de rotina.
- **Motivo:** Otimizar o tempo de resposta e poupar a franquia/limites de requisições. 
- **Padrão:** O sistema DEVE armazenar a data/hora da última sincronização com sucesso e utilizar as tags de filtro da API, como `filtrar_por_data_de`, `filtrar_por_hora_de`, `filtrar_apenas_inclusao`, ou `filtrar_apenas_alteracao`.

## 3. Respeito aos Limites de Consumo (Rate Limits)
- **Regra:** O sistema deve estar preparado para receber erros de estrangulamento (Rate Limit) ou instabilidade (`PROTO_BYEBYE`) e atuar sem intervenção manual.
- **Motivo:** A Omie aplica limites de requisições por IP e App Key. Requisições massivas concorrentes podem ser bloqueadas temporariamente.
- **Padrão:** Implementar um padrão de *Exponential Backoff* (tentativas com atraso progressivo) para qualquer chamada à API que falhe por instabilidade de rede ou bloqueio de limite.

## 4. Garantia de Documento de Origem
- **Regra:** Não inserir lançamentos financeiros avulsos (ex: Contas a Receber) quando a operação derivar de uma venda ou serviço.
- **Motivo:** Cadastrar lançamentos financeiros diretamente ou de forma paralela causa duplicidade, bloqueio de faturação e anomalias contabilísticas/fiscais.
- **Padrão:** A aplicação externa deve gerar o **Documento de Origem** (ex: *Pedido de Venda* ou *Ordem de Serviço*). Ao ser faturado na Omie, o documento gera o financeiro automaticamente.