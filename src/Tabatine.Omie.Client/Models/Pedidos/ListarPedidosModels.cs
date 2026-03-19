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

        [JsonPropertyName("observacoes")]
        public OmiePedidoObservacoes? Observacoes { get; set; }
    }

    public class OmiePedidoObservacoes
    {
        [JsonPropertyName("obs_venda")]
        public string? ObservacaoVenda { get; set; }
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

        [JsonPropertyName("codigo_parcela")]
        public string? CodigoParcela { get; set; }

        [JsonPropertyName("meio_pagamento")]
        public string? MeioPagamento { get; set; }
    }

    public class OmiePedidoItem
    {
        [JsonPropertyName("produto")]
        public OmiePedidoItemProduto Produto { get; set; } = new();

        [JsonPropertyName("imposto")]
        public OmiePedidoItemImposto? Imposto { get; set; }

        [JsonPropertyName("inf_adic")]
        public OmiePedidoItemInfoAdic? InfoAdic { get; set; }
    }

    public class OmiePedidoItemInfoAdic
    {
        [JsonPropertyName("peso_bruto")]
        public decimal PesoBruto { get; set; }

        [JsonPropertyName("peso_liquido")]
        public decimal PesoLiquido { get; set; }
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

        [JsonPropertyName("codigo_tabela_preco")]
        public long? CodigoTabelaPreco { get; set; }

        [JsonPropertyName("percentual_desconto")]
        public decimal PercentualDesconto { get; set; }

        [JsonPropertyName("valor_desconto")]
        public decimal ValorDesconto { get; set; }

        [JsonPropertyName("unidade")]
        public string Unidade { get; set; } = string.Empty;
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

    public class OmiePedidoItemIcms 
    { 
        [JsonPropertyName("valor_icms")] public decimal ValorIcms { get; set; } 
        [JsonPropertyName("base_icms")] public decimal BaseCalculoIcms { get; set; }
        [JsonPropertyName("aliq_icms")] public decimal AliquotaIcms { get; set; }
        [JsonPropertyName("cod_sit_trib_icms")] public string? CstIcms { get; set; }
    }
    public class OmiePedidoItemIpi 
    { 
        [JsonPropertyName("valor_ipi")] public decimal ValorIpi { get; set; } 
        [JsonPropertyName("base_ipi")] public decimal BaseCalculoIpi { get; set; }
        [JsonPropertyName("aliq_ipi")] public decimal AliquotaIpi { get; set; }
        [JsonPropertyName("cod_sit_trib_ipi")] public string? CstIpi { get; set; }
    }
    public class OmiePedidoItemPis 
    { 
        [JsonPropertyName("valor_pis")] public decimal ValorPis { get; set; } 
        [JsonPropertyName("base_pis")] public decimal BaseCalculoPis { get; set; }
        [JsonPropertyName("aliq_pis")] public decimal AliquotaPis { get; set; }
        [JsonPropertyName("cod_sit_trib_pis")] public string? CstPis { get; set; }
    }
    public class OmiePedidoItemCofins 
    { 
        [JsonPropertyName("valor_cofins")] public decimal ValorCofins { get; set; } 
        [JsonPropertyName("base_cofins")] public decimal BaseCalculoCofins { get; set; }
        [JsonPropertyName("aliq_cofins")] public decimal AliquotaCofins { get; set; }
        [JsonPropertyName("cod_sit_trib_cofins")] public string? CstCofins { get; set; }
    }

    public class OmiePedidoFrete
    {
        [JsonPropertyName("valor_frete")]
        public decimal ValorFrete { get; set; }

        [JsonPropertyName("quantidade_volumes")]
        public int QuantidadeVolumes { get; set; }

        [JsonPropertyName("registro_transportador")]
        public string? Transportadora { get; set; }

        [JsonPropertyName("peso_bruto")]
        public decimal PesoBruto { get; set; }

        [JsonPropertyName("peso_liquido")]
        public decimal PesoLiquido { get; set; }

        [JsonPropertyName("previsao_entrega")]
        public string? PrevisaoEntrega { get; set; }

        [JsonPropertyName("modalidade")]
        public string? Modalidade { get; set; }

        [JsonPropertyName("codigo_rastreio")]
        public string? CodigoRastreio { get; set; }

        [JsonPropertyName("link_rastreio")]
        public string? LinkRastreio { get; set; }

        [JsonPropertyName("veiculo_proprio")]
        public string? VeiculoProprio { get; set; }

        [JsonPropertyName("placa")]
        public string? Placa { get; set; }

        [JsonPropertyName("valor_seguro")]
        public decimal ValorSeguro { get; set; }

        [JsonPropertyName("outras_despesas")]
        public decimal OutrasDespesas { get; set; }
    }

    public class OmiePedidoInfoAdic
    {
        [JsonPropertyName("obs_interna")]
        public string? ObservacoesInternas { get; set; }

        [JsonPropertyName("codVend")]
        public long? CodigoVendedor { get; set; }

        [JsonPropertyName("contato")]
        public string? Contato { get; set; }

        [JsonPropertyName("perc_comissao")]
        public decimal PercComissao { get; set; }

        [JsonPropertyName("codigo_conta_corrente")]
        public long? CodigoContaCorrente { get; set; }

        [JsonPropertyName("dados_adicionais_nf")]
        public string? DadosAdicionaisNf { get; set; }

        [JsonPropertyName("numero_pedido_cliente")]
        public string? NumeroPedidoCliente { get; set; }

        [JsonPropertyName("consumidor_final")]
        public string? ConsumidorFinal { get; set; }
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

        [JsonPropertyName("meio_pagamento")]
        public string? MeioPagamento { get; set; }
    }

    public class OmiePedidoTotal
    {
        [JsonPropertyName("valor_total_pedido")]
        public decimal ValorTotalPedido { get; set; }

        [JsonPropertyName("valor_icms")]
        public decimal ValorIcms { get; set; }

        [JsonPropertyName("valor_IPI")]
        public decimal ValorIpi { get; set; }

        [JsonPropertyName("valor_pis")]
        public decimal ValorPis { get; set; }

        [JsonPropertyName("valor_cofins")]
        public decimal ValorCofins { get; set; }

        [JsonPropertyName("base_calculo_icms")]
        public decimal BaseCalculoIcms { get; set; }

        [JsonPropertyName("valor_mercadorias")]
        public decimal ValorMercadorias { get; set; }

        [JsonPropertyName("valor_iss")]
        public decimal ValorIss { get; set; }

        [JsonPropertyName("valor_ir")]
        public decimal ValorIr { get; set; }

        [JsonPropertyName("valor_csll")]
        public decimal ValorCsll { get; set; }

        [JsonPropertyName("valor_inss")]
        public decimal ValorInss { get; set; }

        [JsonPropertyName("valor_descontos")]
        public decimal ValorDescontos { get; set; }

        [JsonPropertyName("valor_ibs")]
        public decimal ValorIbs { get; set; }

        [JsonPropertyName("valor_cbs")]
        public decimal ValorCbs { get; set; }
    }

    public class OmieInfoCadastro
    {
        [JsonPropertyName("dInc")]
        public string? DInc { get; set; }

        [JsonPropertyName("hInc")]
        public string? HInc { get; set; }

        [JsonPropertyName("dAlt")]
        public string? DAlt { get; set; }

        [JsonPropertyName("hAlt")]
        public string? HAlt { get; set; }

        [JsonPropertyName("uInc")]
        public string? UsuarioInclusao { get; set; }

        [JsonPropertyName("uAlt")]
        public string? UsuarioAlteracao { get; set; }

        [JsonPropertyName("faturado")]
        public string? Faturado { get; set; }

        [JsonPropertyName("cancelado")]
        public string? Cancelado { get; set; }

        [JsonPropertyName("devolvido")]
        public string? Devolvido { get; set; }

        [JsonPropertyName("autorizado")]
        public string? Autorizado { get; set; }

        [JsonPropertyName("denegado")]
        public string? Denegado { get; set; }
    }

    public class ListarPedidosResponse : OmieResponse<OmiePedido>
    {
        [JsonPropertyName("pedido_venda_produto")]
        public List<OmiePedido> PedidosVenda { get; set; } = new();
    }
}
