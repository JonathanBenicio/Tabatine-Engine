using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tabatine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCondicaoPagamentoTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CondicaoPagamentoId",
                table: "PedidosVenda",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FreteModalidade",
                table: "PedidosVenda",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MeioPagamento",
                table: "PedidosVenda",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ObservacoesInternas",
                table: "PedidosVenda",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorCsll",
                table: "PedidosVenda",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorInss",
                table: "PedidosVenda",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorIr",
                table: "PedidosVenda",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorIss",
                table: "PedidosVenda",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Ambiente",
                table: "NotasFiscais",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Finalidade",
                table: "NotasFiscais",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "IcmsBaseCalculo",
                table: "NotasFiscais",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "IcmsValor",
                table: "NotasFiscais",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "InformacoesComplementares",
                table: "NotasFiscais",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InformacoesFisco",
                table: "NotasFiscais",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "IssqnBaseCalculo",
                table: "NotasFiscais",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Modelo",
                table: "NotasFiscais",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TipoOperacao",
                table: "NotasFiscais",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorCofins",
                table: "NotasFiscais",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorDesconto",
                table: "NotasFiscais",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorFrete",
                table: "NotasFiscais",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorIpi",
                table: "NotasFiscais",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorOutrasDespesas",
                table: "NotasFiscais",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorPis",
                table: "NotasFiscais",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorProd",
                table: "NotasFiscais",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorSeguro",
                table: "NotasFiscais",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "AliqCofins",
                table: "ItensPedido",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "AliqIcms",
                table: "ItensPedido",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "AliqIpi",
                table: "ItensPedido",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "AliqPis",
                table: "ItensPedido",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "BaseCofins",
                table: "ItensPedido",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "BaseIcms",
                table: "ItensPedido",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "BaseIpi",
                table: "ItensPedido",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "BasePis",
                table: "ItensPedido",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "CstIcms",
                table: "ItensPedido",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CstIpi",
                table: "ItensPedido",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AliqIcms",
                table: "ItensNotaFiscal",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "AliqIpi",
                table: "ItensNotaFiscal",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "BaseIcms",
                table: "ItensNotaFiscal",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "BaseIpi",
                table: "ItensNotaFiscal",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "CstIcms",
                table: "ItensNotaFiscal",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CstIpi",
                table: "ItensNotaFiscal",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorCofins",
                table: "ItensNotaFiscal",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorIpi",
                table: "ItensNotaFiscal",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorPis",
                table: "ItensNotaFiscal",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "CondicoesPagamento",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Codigo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    QuantidadeParcelas = table.Column<int>(type: "integer", nullable: false),
                    DiaFixo = table.Column<int>(type: "integer", nullable: false),
                    OmieId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OmieUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CondicoesPagamento", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PedidosVenda_CondicaoPagamentoId",
                table: "PedidosVenda",
                column: "CondicaoPagamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_CondicoesPagamento_Codigo",
                table: "CondicoesPagamento",
                column: "Codigo",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PedidosVenda_CondicoesPagamento_CondicaoPagamentoId",
                table: "PedidosVenda",
                column: "CondicaoPagamentoId",
                principalTable: "CondicoesPagamento",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PedidosVenda_CondicoesPagamento_CondicaoPagamentoId",
                table: "PedidosVenda");

            migrationBuilder.DropTable(
                name: "CondicoesPagamento");

            migrationBuilder.DropIndex(
                name: "IX_PedidosVenda_CondicaoPagamentoId",
                table: "PedidosVenda");

            migrationBuilder.DropColumn(
                name: "CondicaoPagamentoId",
                table: "PedidosVenda");

            migrationBuilder.DropColumn(
                name: "FreteModalidade",
                table: "PedidosVenda");

            migrationBuilder.DropColumn(
                name: "MeioPagamento",
                table: "PedidosVenda");

            migrationBuilder.DropColumn(
                name: "ObservacoesInternas",
                table: "PedidosVenda");

            migrationBuilder.DropColumn(
                name: "ValorCsll",
                table: "PedidosVenda");

            migrationBuilder.DropColumn(
                name: "ValorInss",
                table: "PedidosVenda");

            migrationBuilder.DropColumn(
                name: "ValorIr",
                table: "PedidosVenda");

            migrationBuilder.DropColumn(
                name: "ValorIss",
                table: "PedidosVenda");

            migrationBuilder.DropColumn(
                name: "Ambiente",
                table: "NotasFiscais");

            migrationBuilder.DropColumn(
                name: "Finalidade",
                table: "NotasFiscais");

            migrationBuilder.DropColumn(
                name: "IcmsBaseCalculo",
                table: "NotasFiscais");

            migrationBuilder.DropColumn(
                name: "IcmsValor",
                table: "NotasFiscais");

            migrationBuilder.DropColumn(
                name: "InformacoesComplementares",
                table: "NotasFiscais");

            migrationBuilder.DropColumn(
                name: "InformacoesFisco",
                table: "NotasFiscais");

            migrationBuilder.DropColumn(
                name: "IssqnBaseCalculo",
                table: "NotasFiscais");

            migrationBuilder.DropColumn(
                name: "Modelo",
                table: "NotasFiscais");

            migrationBuilder.DropColumn(
                name: "TipoOperacao",
                table: "NotasFiscais");

            migrationBuilder.DropColumn(
                name: "ValorCofins",
                table: "NotasFiscais");

            migrationBuilder.DropColumn(
                name: "ValorDesconto",
                table: "NotasFiscais");

            migrationBuilder.DropColumn(
                name: "ValorFrete",
                table: "NotasFiscais");

            migrationBuilder.DropColumn(
                name: "ValorIpi",
                table: "NotasFiscais");

            migrationBuilder.DropColumn(
                name: "ValorOutrasDespesas",
                table: "NotasFiscais");

            migrationBuilder.DropColumn(
                name: "ValorPis",
                table: "NotasFiscais");

            migrationBuilder.DropColumn(
                name: "ValorProd",
                table: "NotasFiscais");

            migrationBuilder.DropColumn(
                name: "ValorSeguro",
                table: "NotasFiscais");

            migrationBuilder.DropColumn(
                name: "AliqCofins",
                table: "ItensPedido");

            migrationBuilder.DropColumn(
                name: "AliqIcms",
                table: "ItensPedido");

            migrationBuilder.DropColumn(
                name: "AliqIpi",
                table: "ItensPedido");

            migrationBuilder.DropColumn(
                name: "AliqPis",
                table: "ItensPedido");

            migrationBuilder.DropColumn(
                name: "BaseCofins",
                table: "ItensPedido");

            migrationBuilder.DropColumn(
                name: "BaseIcms",
                table: "ItensPedido");

            migrationBuilder.DropColumn(
                name: "BaseIpi",
                table: "ItensPedido");

            migrationBuilder.DropColumn(
                name: "BasePis",
                table: "ItensPedido");

            migrationBuilder.DropColumn(
                name: "CstIcms",
                table: "ItensPedido");

            migrationBuilder.DropColumn(
                name: "CstIpi",
                table: "ItensPedido");

            migrationBuilder.DropColumn(
                name: "AliqIcms",
                table: "ItensNotaFiscal");

            migrationBuilder.DropColumn(
                name: "AliqIpi",
                table: "ItensNotaFiscal");

            migrationBuilder.DropColumn(
                name: "BaseIcms",
                table: "ItensNotaFiscal");

            migrationBuilder.DropColumn(
                name: "BaseIpi",
                table: "ItensNotaFiscal");

            migrationBuilder.DropColumn(
                name: "CstIcms",
                table: "ItensNotaFiscal");

            migrationBuilder.DropColumn(
                name: "CstIpi",
                table: "ItensNotaFiscal");

            migrationBuilder.DropColumn(
                name: "ValorCofins",
                table: "ItensNotaFiscal");

            migrationBuilder.DropColumn(
                name: "ValorIpi",
                table: "ItensNotaFiscal");

            migrationBuilder.DropColumn(
                name: "ValorPis",
                table: "ItensNotaFiscal");
        }
    }
}
