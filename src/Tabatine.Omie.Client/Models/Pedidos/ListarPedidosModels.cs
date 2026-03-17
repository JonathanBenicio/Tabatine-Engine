using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace Tabatine.Omie.Client.Models.Pedidos
{
    public class ListarPedidosParam
    {
        [JsonPropertyName("pagina")]
        public int Pagina { get; set; } = 1;

        [JsonPropertyName("registros_por_pagina")]
        public int RegistrosPorPagina { get; set; } = 500;

        [JsonPropertyName("filtrar_por_data_de")]
        public string? FiltrarPorDataDe { get; set; }

        [JsonPropertyName("filtrar_por_data_ate")]
        public string? FiltrarPorDataAte { get; set; }

        [JsonPropertyName("apenas_importado_api")]
        public string ApenasImportadoApi { get; set; } = "N";
    }

    public class OmiePedido
    {
        [JsonPropertyName("cabecalho")]
        public OmiePedidoCabecalho Cabecalho { get; set; } = new();

        [JsonPropertyName("det")]
        public List<OmiePedidoItem> Det { get; set; } = new();

        [JsonPropertyName("total_pedido")]
        public OmiePedidoTotal TotalPedido { get; set; } = new();

        [JsonPropertyName("frete")]
        public OmiePedidoFrete? Frete { get; set; }

        [JsonPropertyName("informacoes_adicionais")]
        public OmiePedidoInfoAdic? InformacoesAdicionais { get; set; }

        [JsonPropertyName("lista_parcelas")]
        public OmiePedidoParcelas? ListaParcelas { get; set; }

        [JsonPropertyName("infoCadastro")]
        public OmieInfoCadastro? InfoCadastro { get; set; }
    }

    public class OmiePedidoCabecalho
    {
        [JsonPropertyName("codigo_pedido")]
        public long CodigoPedido { get; set; }

        [JsonPropertyName("numero_pedido")]
        public string NumeroPedido { get; set; } = string.Empty;

        [JsonPropertyName("codigo_cliente")]
        public long CodigoCliente { get; set; }

        [JsonPropertyName("etapa")]
        public string Etapa { get; set; } = string.Empty;

        [JsonPropertyName("quantidade_itens")]
        public int QuantidadeItens { get; set; }

        [JsonPropertyName("data_previsao")]
        public string? DataPrevisao { get; set; }

        [JsonPropertyName("codigo_conta_corrente")]
        public long? CodigoContaCorrente { get; set; }
    }

    public class OmiePedidoItem
    {
        [JsonPropertyName("produto")]
        public OmiePedidoItemProduto Produto { get; set; } = new();

        [JsonPropertyName("imposto")]
        public OmiePedidoItemImposto? Imposto { get; set; }
    }

    public class OmiePedidoItemProduto
    {
        [JsonPropertyName("codigo_produto")]
        public long CodigoProduto { get; set; }

        [JsonPropertyName("quantidade")]
        public decimal Quantidade { get; set; }

        [JsonPropertyName("valor_unitario")]
        public decimal ValorUnitario { get; set; }

        [JsonPropertyName("valor_total")]
        public decimal ValorTotal { get; set; }

        [JsonPropertyName("percentual_desconto")]
        public decimal PercentualDesconto { get; set; }

        [JsonPropertyName("valor_desconto")]
        public decimal ValorDesconto { get; set; }
    }

    public class OmiePedidoItemImposto
    {
        [JsonPropertyName("icms")]
        public OmiePedidoItemIcms? Icms { get; set; }

        [JsonPropertyName("ipi")]
        public OmiePedidoItemIpi? Ipi { get; set; }

        [JsonPropertyName("pis_padrao")]
        public OmiePedidoItemPis? Pis { get; set; }

        [JsonPropertyName("cofins_padrao")]
        public OmiePedidoItemCofins? Cofins { get; set; }
    }

    public class OmiePedidoItemIcms { [JsonPropertyName("valor_icms")] public decimal ValorIcms { get; set; } }
    public class OmiePedidoItemIpi { [JsonPropertyName("valor_ipi")] public decimal ValorIpi { get; set; } }
    public class OmiePedidoItemPis { [JsonPropertyName("valor_pis")] public decimal ValorPis { get; set; } }
    public class OmiePedidoItemCofins { [JsonPropertyName("valor_cofins")] public decimal ValorCofins { get; set; } }

    public class OmiePedidoFrete
    {
        [JsonPropertyName("valor_frete")]
        public decimal ValorFrete { get; set; }

        [JsonPropertyName("quantidade_volumes")]
        public int QuantidadeVolumes { get; set; }

        [JsonPropertyName("registro_transportador")]
        public string? Transportadora { get; set; }
    }

    public class OmiePedidoInfoAdic
    {
        [JsonPropertyName("dados_adicionais_nf")]
        public string? ObservacoesVenda { get; set; }

        [JsonPropertyName("codVend")]
        public long? CodigoVendedor { get; set; }
    }

    public class OmiePedidoParcelas
    {
        [JsonPropertyName("parcela")]
        public List<OmiePedidoParcela> Parcelas { get; set; } = new();
    }

    public class OmiePedidoParcela
    {
        [JsonPropertyName("valor")]
        public decimal Valor { get; set; }

        [JsonPropertyName("data_vencimento")]
        public string DataVencimento { get; set; } = string.Empty;

        [JsonPropertyName("numero_parcela")]
        public int NumeroParcela { get; set; }

        [JsonPropertyName("percentual")]
        public decimal Percentual { get; set; }
    }

    public class OmiePedidoTotal
    {
        [JsonPropertyName("valor_total_pedido")]
        public decimal ValorTotalPedido { get; set; }
    }

    public class OmieInfoCadastro
    {
        [JsonPropertyName("dAlt")]
        public string? DAlt { get; set; }

        [JsonPropertyName("hAlt")]
        public string? HAlt { get; set; }

        [JsonPropertyName("uInc")]
        public string? UsuarioInclusao { get; set; }

        [JsonPropertyName("faturado")]
        public string? Faturado { get; set; }
    }

    public class ListarPedidosResponse : OmieResponse<OmiePedido>
    {
        [JsonPropertyName("pedido_venda_produto")]
        public List<OmiePedido> PedidosVenda { get; set; } = new();
    }
}
