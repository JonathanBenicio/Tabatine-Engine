using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace Tabatine.Omie.Client.Models.NotasFiscais
{
    public class ListarNotasFiscaisParam
    {
        [JsonPropertyName("pagina")]
        public int Pagina { get; set; } = 1;

        [JsonPropertyName("registros_por_pagina")]
        public int RegistrosPorPagina { get; set; } = 500;

        [JsonPropertyName("ordenar_por")]
        public string OrdenarPor { get; set; } = "CODIGO";

        [JsonPropertyName("filtrar_por_data_de")]
        public string? FiltrarPorDataDe { get; set; }

        [JsonPropertyName("filtrar_por_data_ate")]
        public string? FiltrarPorDataAte { get; set; }

        [JsonPropertyName("dEmiInicial")]
        public string? DataEmissaoDe { get; set; }

        [JsonPropertyName("dEmiFinal")]
        public string? DataEmissaoAte { get; set; }

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
    }

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
    }

    public class OmieNfIssqntot
    {
        [JsonPropertyName("vISS")]
        public decimal ValorIss { get; set; }
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
