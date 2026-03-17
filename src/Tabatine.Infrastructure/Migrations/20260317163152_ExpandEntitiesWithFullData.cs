using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tabatine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ExpandEntitiesWithFullData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FamiliaProduto",
                table: "Produtos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PesoBruto",
                table: "Produtos",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PesoLiquido",
                table: "Produtos",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "UnidadeMedida",
                table: "Produtos",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DataPrevisao",
                table: "PedidosVenda",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<long>(
                name: "CodigoVendedor",
                table: "PedidosVenda",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Faturado",
                table: "PedidosVenda",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ObservacoesVenda",
                table: "PedidosVenda",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QuantidadeVolumes",
                table: "PedidosVenda",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Transportadora",
                table: "PedidosVenda",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UsuarioInclusao",
                table: "PedidosVenda",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorFrete",
                table: "PedidosVenda",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "HoraEmissao",
                table: "NotasFiscais",
                type: "interval",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorCofinsRetido",
                table: "NotasFiscais",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorCsll",
                table: "NotasFiscais",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorIr",
                table: "NotasFiscais",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorIss",
                table: "NotasFiscais",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorPisRetido",
                table: "NotasFiscais",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PercentualDesconto",
                table: "ItensPedido",
                type: "numeric(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorCofins",
                table: "ItensPedido",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorDesconto",
                table: "ItensPedido",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorIcms",
                table: "ItensPedido",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorIpi",
                table: "ItensPedido",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorPis",
                table: "ItensPedido",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<string>(
                name: "InscricaoEstadual",
                table: "Clientes",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Estado",
                table: "Clientes",
                type: "character varying(2)",
                maxLength: 2,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Cep",
                table: "Clientes",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Bairro",
                table: "Clientes",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Endereco",
                table: "Clientes",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EnderecoComplemento",
                table: "Clientes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EnderecoNumero",
                table: "Clientes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InscricaoMunicipal",
                table: "Clientes",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "OptanteSimplesNacional",
                table: "Clientes",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FamiliaProduto",
                table: "Produtos");

            migrationBuilder.DropColumn(
                name: "PesoBruto",
                table: "Produtos");

            migrationBuilder.DropColumn(
                name: "PesoLiquido",
                table: "Produtos");

            migrationBuilder.DropColumn(
                name: "UnidadeMedida",
                table: "Produtos");

            migrationBuilder.DropColumn(
                name: "CodigoVendedor",
                table: "PedidosVenda");

            migrationBuilder.DropColumn(
                name: "Faturado",
                table: "PedidosVenda");

            migrationBuilder.DropColumn(
                name: "ObservacoesVenda",
                table: "PedidosVenda");

            migrationBuilder.DropColumn(
                name: "QuantidadeVolumes",
                table: "PedidosVenda");

            migrationBuilder.DropColumn(
                name: "Transportadora",
                table: "PedidosVenda");

            migrationBuilder.DropColumn(
                name: "UsuarioInclusao",
                table: "PedidosVenda");

            migrationBuilder.DropColumn(
                name: "ValorFrete",
                table: "PedidosVenda");

            migrationBuilder.DropColumn(
                name: "HoraEmissao",
                table: "NotasFiscais");

            migrationBuilder.DropColumn(
                name: "ValorCofinsRetido",
                table: "NotasFiscais");

            migrationBuilder.DropColumn(
                name: "ValorCsll",
                table: "NotasFiscais");

            migrationBuilder.DropColumn(
                name: "ValorIr",
                table: "NotasFiscais");

            migrationBuilder.DropColumn(
                name: "ValorIss",
                table: "NotasFiscais");

            migrationBuilder.DropColumn(
                name: "ValorPisRetido",
                table: "NotasFiscais");

            migrationBuilder.DropColumn(
                name: "PercentualDesconto",
                table: "ItensPedido");

            migrationBuilder.DropColumn(
                name: "ValorCofins",
                table: "ItensPedido");

            migrationBuilder.DropColumn(
                name: "ValorDesconto",
                table: "ItensPedido");

            migrationBuilder.DropColumn(
                name: "ValorIcms",
                table: "ItensPedido");

            migrationBuilder.DropColumn(
                name: "ValorIpi",
                table: "ItensPedido");

            migrationBuilder.DropColumn(
                name: "ValorPis",
                table: "ItensPedido");

            migrationBuilder.DropColumn(
                name: "Bairro",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "Endereco",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "EnderecoComplemento",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "EnderecoNumero",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "InscricaoMunicipal",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "OptanteSimplesNacional",
                table: "Clientes");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DataPrevisao",
                table: "PedidosVenda",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "InscricaoEstadual",
                table: "Clientes",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Estado",
                table: "Clientes",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2)",
                oldMaxLength: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Cep",
                table: "Clientes",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10,
                oldNullable: true);
        }
    }
}
