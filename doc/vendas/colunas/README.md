# Colunas da Tela de Vendas

Documentação dos campos exibidos na tabela de vendas (`VendasTable.tsx`), com origem dos dados e observações.

> [!NOTE]
> Os dados são obtidos via endpoint `ListarPedidos` da API Omie e "achatados" (flattened) no `useVendasStore.ts` — cada linha representa **um produto** dentro de um pedido.

---

## Legenda de Origem

| Símbolo | Significado |
|---------|-------------|
| ✅ API | Valor vem diretamente da API Omie |
| ⚠️ Aproximado | Valor usa campo da API que não corresponde exatamente ao dado desejado |
| ❌ Hardcoded | Valor fixo definido no código, não consultado em nenhuma API |

---

## Mapeamento das Colunas

| # | Coluna | Campo no Store | Origem | Fonte na API Omie | Observações |
|---|--------|----------------|--------|--------------------|-------------|
| 1 | 📅 Data | `data` | ✅ API | `cabecalho.data_previsao` ou `cabecalho.data_pedido` | Fallback para `data_pedido` se `data_previsao` não existir |
| 2 | 👥 Cliente | `cliente` | ✅ API | `cabecalho.codigo_cliente` | Exibe o **código numérico** do cliente, não o nome. Seria necessário um join com a API de Clientes para exibir o nome |
| 3 | 👤 Vendedor | `vendedor` | ✅ API | `cabecalho.codigo_vendedor` | Exibe o **código numérico** do vendedor, não o nome |
| 4 | 📦 Pedido | `pedido` | ✅ API | `cabecalho.numero_pedido` | Número do pedido de venda |
| 5 | 📄 NF | `nf` | ✅ API | `infoCadastro.numero_nfe` | Número da nota fiscal eletrônica associada |
| 6 | 🛒 Produto | `produto` | ✅ API | `det[].produto.descricao` ou `det[].produto.xProd` | Descrição do produto no item do pedido |
| 7 | 📦 Und | `und` | ✅ API | `det[].produto.unidade` ou `det[].produto.uCom` | Unidade de medida (ex: UN, CX, KG) |
| 8 | 💰 Valor Venda | `valorVenda` | ✅ API | `det[].produto.valor_unitario` ou `det[].produto.vUnCom` | Valor unitário do produto |
| 9 | 💳 Cond. Pagto. | `condPagto` | ✅ API | `cabecalho.codigo_parcela` | Código da condição de pagamento |
| 10 | 🚚 Frete | `frete` | ✅ API | `frete.valor_frete` | Valor do frete do pedido |
| 11 | 📈 Coms. % | `percComissao` | ⚠️ Aproximado | `det[].produto.perc_desconto` | **Não é a comissão real.** Usa o percentual de desconto do produto como fallback. A API `ListarPedidos` não retorna comissão diretamente |
| 12 | 🎯 Valor Total | `valorTotal` | ✅ API | `det[].produto.valor_mercadoria` ou `det[].produto.vProd` | Valor total do item (quantidade × valor unitário) |
| 13 | 🏦 Forma Pg | `formaPg` | ✅ API | `cabecalho.forma_pagamento` | Forma de pagamento do pedido |
| 14 | 🏛️ Banco | `banco` | ✅ API | `cabecalho.conta_corrente` | Conta corrente / banco vinculado |
| 15 | 💰 Parcela 1 | `parcela1.valor` | ✅ API | `lista_parcelas.parcela[0].valor` | Valor da 1ª parcela |
| 16 | 📅 Venc. 1 | `parcela1.vencimento` | ✅ API | `lista_parcelas.parcela[0].data_vencimento` | Data de vencimento da 1ª parcela |
| 17 | 🚦 Status Venc. | `vencimentoStatus` | ⚠️ Aproximado | `cabecalho.etapa` | **Não é o status de vencimento real.** Usa a `etapa` do pedido (ex: "20 - Faturar", "50 - Entregue"), que indica o estágio do pedido e não se a parcela venceu |
| 18 | 💰 Parcela 2 | `parcela2.valor` | ✅ API | `lista_parcelas.parcela[1].valor` | Valor da 2ª parcela |
| 19 | 📅 Venc. 2 | `parcela2.vencimento` | ✅ API | `lista_parcelas.parcela[1].data_vencimento` | Data de vencimento da 2ª parcela |
| 20 | 💰 Parcela 3 | `parcela3.valor` | ✅ API | `lista_parcelas.parcela[2].valor` | Valor da 3ª parcela |
| 21 | 📅 Venc. 3 | `parcela3.vencimento` | ✅ API | `lista_parcelas.parcela[2].data_vencimento` | Data de vencimento da 3ª parcela |
| 22 | 🎗️ Status Comissão | `statusComissao` | ❌ Hardcoded | — | **Sempre exibe `PENDENTE`**. Valor fixo no código (`useVendasStore.ts:225`). Não existe consulta a nenhuma API para esse dado |

---

## Campos que Precisam de Atenção

### ❌ `statusComissao` — Totalmente hardcoded
```typescript
// useVendasStore.ts:225
statusComissao: 'PENDENTE'
```
Toda venda aparece como "PENDENTE". Para tornar funcional, seria necessário:
- Uma API ou tabela separada de comissões, ou
- Controle manual pelo usuário marcando como "PAGO"

### ⚠️ `percComissao` — Usando campo errado
```typescript
// useVendasStore.ts:217
percComissao: prod.perc_desconto || 0
```
Usa `perc_desconto` (desconto do produto) como substituto da comissão. A API `ListarPedidos` não retorna o percentual de comissão do vendedor.

### ⚠️ `vencimentoStatus` — Semântica incorreta
```typescript
// useVendasStore.ts:224
vencimentoStatus: cabecalho.etapa || 'Pendente'
```
O campo `etapa` indica o **estágio do pedido** no fluxo de venda (ex: "10 - Separar", "20 - Faturar"), não se as parcelas estão vencidas ou em dia.

---

## Arquivos Relevantes

| Arquivo | Responsabilidade |
|---------|-----------------|
| `src/store/useVendasStore.ts` | Fetch da API, cache, flatten dos dados |
| `src/components/VendasTable.tsx` | Renderização da tabela, formatação visual |
| `src/components/Pagination.tsx` | Componente de paginação |

---

*Documentação gerada em 15/03/2026.*
