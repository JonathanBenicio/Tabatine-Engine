using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace Tabatine.Omie.Client.Models.NotasFiscais
{
    public class ListarNotasFiscaisParam
    {
        [JsonPropertyName("pagina")]
        public int Pagina { get; set; } = 1;

        [JsonPropertyName("registros_por_pagina")]
        public int RegistrosPorPagina { get; set; } = 100;

        [JsonPropertyName("ordenar_por")]
        public string OrdenarPor { get; set; } = "CODIGO";

        [JsonPropertyName("dEmiInicial")]
        public string? DataEmissaoDe { get; set; }

        [JsonPropertyName("dEmiFinal")]
        public string? DataEmissaoAte { get; set; }

        [JsonPropertyName("filtrar_por_data_de")]
        public string? FiltrarPorDataDe { get; set; }

        [JsonPropertyName("filtrar_por_data_ate")]
        public string? FiltrarPorDataAte { get; set; }

        [JsonPropertyName("filtrar_apenas_inclusao")]
        public string? FiltrarApenasInclusao { get; set; }

        [JsonPropertyName("filtrar_apenas_alteracao")]
        public string? FiltrarApenasAlteracao { get; set; }

        [JsonPropertyName("apenas_importado_api")]
        public string ApenasImportadoApi { get; set; } = "N";


    }

    public class OmieNotaFiscal
    {
        [JsonPropertyName("ide")]
        public OmieNfIde Ide { get; set; } = new();

        [JsonPropertyName("compl")]
        public OmieNfCompl Compl { get; set; } = new();

        [JsonPropertyName("det")]
        public List<OmieNfDet> Det { get; set; } = new();

        [JsonPropertyName("total")]
        public OmieNfTotal Total { get; set; } = new();

        [JsonPropertyName("nfDestInt")]
        public OmieNfDestInt Destinatario { get; set; } = new();

        [JsonPropertyName("titulos")]
        public List<OmieNfTitulo>? Titulos { get; set; }

        [JsonPropertyName("info")]
        public OmieInfoCadastro? Info { get; set; }
    }
    public class OmieNfDet
    {
        [JsonPropertyName("prod")]
        public OmieNfDetProd Prod { get; set; } = new();

        [JsonPropertyName("imposto")]
        public OmieNfDetImposto? Imposto { get; set; }
    }

    public class OmieNfDetImposto
    {
        [JsonPropertyName("ICMS")]
        public OmieNfDetIcms? Icms { get; set; }

        [JsonPropertyName("IPI")]
        public OmieNfDetIpi? Ipi { get; set; }

        [JsonPropertyName("PIS")]
        public OmieNfDetPis? Pis { get; set; }

        [JsonPropertyName("COFINS")]
        public OmieNfDetCofins? Cofins { get; set; }

        [JsonPropertyName("IBS")]
        public OmieNfDetIbs? Ibs { get; set; }

        [JsonPropertyName("CBS")]
        public OmieNfDetCbs? Cbs { get; set; }

        [JsonPropertyName("ibs_cbs")]
        public OmieNfDetIbsCbs? IbsCbs { get; set; }
    }

    public class OmieNfDetIbs
    {
        [JsonPropertyName("vIBS")] public decimal ValorIbs { get; set; }
        [JsonPropertyName("pIBS")] public decimal AliquotaIbs { get; set; }
    }

    public class OmieNfDetCbs
    {
        [JsonPropertyName("vCBS")] public decimal ValorCbs { get; set; }
        [JsonPropertyName("pCBS")] public decimal AliquotaCbs { get; set; }
    }

    public class OmieNfDetIbsCbs
    {
        [JsonPropertyName("vBC_IBS_CBS")] public decimal BaseIbsCbs { get; set; }
    }

    public class OmieNfDetIcms
    {
        [JsonPropertyName("vBC")] public decimal Base { get; set; }
        [JsonPropertyName("pICMS")] public decimal Aliquota { get; set; }
        [JsonPropertyName("vICMS")] public decimal Valor { get; set; }
        [JsonPropertyName("CST")] public string? Cst { get; set; }
    }

    public class OmieNfDetIpi
    {
        [JsonPropertyName("vBC")] public decimal Base { get; set; }
        [JsonPropertyName("pIPI")] public decimal Aliquota { get; set; }
        [JsonPropertyName("vIPI")] public decimal Valor { get; set; }
        [JsonPropertyName("CST")] public string? Cst { get; set; }
    }

    public class OmieNfDetPis { [JsonPropertyName("vPIS")] public decimal Valor { get; set; } }
    public class OmieNfDetCofins { [JsonPropertyName("vCOFINS")] public decimal Valor { get; set; } }

    public class OmieNfDetProd
    {
        [JsonPropertyName("cProd")]
        public string Codigo { get; set; } = string.Empty;

        [JsonPropertyName("xProd")]
        public string Descricao { get; set; } = string.Empty;

        [JsonPropertyName("qCom")]
        public decimal Quantidade { get; set; }

        [JsonPropertyName("vUnCom")]
        public decimal ValorUnitario { get; set; }

        [JsonPropertyName("vTotItem")]
        public decimal ValorTotal { get; set; }

        [JsonPropertyName("CFOP")]
        public string Cfop { get; set; } = string.Empty;

        [JsonPropertyName("NCM")]
        public string Ncm { get; set; } = string.Empty;
    }

    public class OmieNfTitulo
    {
        [JsonPropertyName("cNumTitulo")]
        public string Numero { get; set; } = string.Empty;

        [JsonPropertyName("nCodTitulo")]
        public long OmieIdTitulo { get; set; }

        [JsonPropertyName("dDtVenc")]
        public string DataVencimento { get; set; } = string.Empty;

        [JsonPropertyName("nValorTitulo")]
        public decimal Valor { get; set; }

        [JsonPropertyName("nParcela")]
        public int Parcela { get; set; }

        [JsonPropertyName("nCodVendedor")]
        public long CodigoVendedor { get; set; }
    }

    public class OmieNfIde
    {
        [JsonPropertyName("nNF")]
        public string Numero { get; set; } = string.Empty;

        [JsonPropertyName("dEmi")]
        public string DataEmissao { get; set; } = string.Empty;

        [JsonPropertyName("hEmi")]
        public string? HoraEmissao { get; set; }

        [JsonPropertyName("cDeneg")]
        public string Denegada { get; set; } = "N";

        [JsonPropertyName("cSitNF")]
        public string Situacao { get; set; } = string.Empty;

        [JsonPropertyName("serie")]
        public string Serie { get; set; } = string.Empty;

        [JsonPropertyName("mod")]
        public string Modelo { get; set; } = "55";

        [JsonPropertyName("tpNF")]
        public string TipoNf { get; set; } = string.Empty;

        [JsonPropertyName("finNFe")]
        public string Finalidade { get; set; } = string.Empty;

        [JsonPropertyName("tpAmb")]
        public string Ambiente { get; set; } = string.Empty;

        [JsonPropertyName("dSaiEnt")]
        public string? DataSaida { get; set; }

        [JsonPropertyName("hSaiEnt")]
        public string? HoraSaida { get; set; }
    }

    public class OmieNfCompl
    {
        [JsonPropertyName("nIdNF")]
        public long IdNf { get; set; }

        [JsonPropertyName("cChaveNFe")]
        public string ChaveNfe { get; set; } = string.Empty;

        [JsonPropertyName("nIdPedido")]
        public long? IdPedido { get; set; }

        [JsonPropertyName("xNatureza")]
        public string XNatureza { get; set; } = string.Empty;

        [JsonPropertyName("cInfCpl")]
        public string? InformacoesComplementares { get; set; }

        [JsonPropertyName("cInfAdFisco")]
        public string? InformacoesFisco { get; set; }

        [JsonPropertyName("nIdTransp")]
        public long? IdTransportadora { get; set; }
    }

    public class OmieNfTotal
    {
        [JsonPropertyName("ICMSTot")]
        public OmieNfIcmstot IcmsTot { get; set; } = new();

        [JsonPropertyName("ISSQNtot")]
        public OmieNfIssqntot? IssqnTot { get; set; }

        [JsonPropertyName("retTrib")]
        public OmieNfRetTrib? RetTrib { get; set; }
    }

    public class OmieNfIcmstot
    {
        [JsonPropertyName("vNF")]
        public decimal ValorNota { get; set; }

        [JsonPropertyName("vBC")]
        public decimal BaseCalculoIcms { get; set; }

        [JsonPropertyName("vICMS")]
        public decimal ValorIcms { get; set; }

        [JsonPropertyName("vIPI")]
        public decimal ValorIpi { get; set; }

        [JsonPropertyName("vPIS")]
        public decimal ValorPis { get; set; }

        [JsonPropertyName("vCOFINS")]
        public decimal ValorCofins { get; set; }

        [JsonPropertyName("vProd")]
        public decimal ValorProdutos { get; set; }

        [JsonPropertyName("vFrete")]
        public decimal ValorFrete { get; set; }

        [JsonPropertyName("vSeg")]
        public decimal ValorSeguro { get; set; }

        [JsonPropertyName("vDesc")]
        public decimal ValorDesconto { get; set; }

        [JsonPropertyName("vOutro")]
        public decimal ValorOutrasDespesas { get; set; }

        [JsonPropertyName("vIBS")]
        public decimal ValorIbs { get; set; }

        [JsonPropertyName("vCBS")]
        public decimal ValorCbs { get; set; }
    }

    public class OmieNfIssqntot
    {
        [JsonPropertyName("vISS")]
        public decimal ValorIss { get; set; }

        [JsonPropertyName("vBC")]
        public decimal BaseCalculo { get; set; }
    }

    public class OmieNfRetTrib
    {
        [JsonPropertyName("vIRRF")]
        public decimal ValorIrrf { get; set; }

        [JsonPropertyName("vRetCSLL")]
        public decimal ValorCsll { get; set; }

        [JsonPropertyName("vRetPIS")]
        public decimal ValorPis { get; set; }

        [JsonPropertyName("vRetCOFINS")]
        public decimal ValorCofins { get; set; }
    }

    public class OmieNfDestInt
    {
        [JsonPropertyName("nCodCli")]
        public long CodigoCliente { get; set; }

        [JsonPropertyName("cRazao")]
        public string RazaoSocial { get; set; } = string.Empty;
    }

    public class OmieInfoCadastro
    {
        [JsonPropertyName("dAlt")]
        public string? DAlt { get; set; }

        [JsonPropertyName("hAlt")]
        public string? HAlt { get; set; }
    }

    public class ListarNotasFiscaisResponse : OmieResponse<OmieNotaFiscal>
    {
        [JsonPropertyName("nfCadastro")]
        public List<OmieNotaFiscal> NotasFiscais { get; set; } = new();
    }
}
