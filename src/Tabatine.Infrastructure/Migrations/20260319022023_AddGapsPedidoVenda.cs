using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tabatine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGapsPedidoVenda : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CodigoRastreio",
                table: "PedidosVenda",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConsumidorFinal",
                table: "PedidosVenda",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DadosAdicionaisNf",
                table: "PedidosVenda",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LinkRastreio",
                table: "PedidosVenda",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NumeroPedidoCliente",
                table: "PedidosVenda",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Placa",
                table: "PedidosVenda",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorCbs",
                table: "PedidosVenda",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorDesconto",
                table: "PedidosVenda",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorIbs",
                table: "PedidosVenda",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorOutrasDespesas",
                table: "PedidosVenda",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorSeguro",
                table: "PedidosVenda",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "VeiculoProprio",
                table: "PedidosVenda",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "IcmsValor",
                table: "NotasFiscais",
                type: "numeric(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AddColumn<DateTime>(
                name: "DataSaida",
                table: "NotasFiscais",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "HoraSaida",
                table: "NotasFiscais",
                type: "interval",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "IdTransportadora",
                table: "NotasFiscais",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorCbs",
                table: "NotasFiscais",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorIbs",
                table: "NotasFiscais",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CodigoRastreio",
                table: "PedidosVenda");

            migrationBuilder.DropColumn(
                name: "ConsumidorFinal",
                table: "PedidosVenda");

            migrationBuilder.DropColumn(
                name: "DadosAdicionaisNf",
                table: "PedidosVenda");

            migrationBuilder.DropColumn(
                name: "LinkRastreio",
                table: "PedidosVenda");

            migrationBuilder.DropColumn(
                name: "NumeroPedidoCliente",
                table: "PedidosVenda");

            migrationBuilder.DropColumn(
                name: "Placa",
                table: "PedidosVenda");

            migrationBuilder.DropColumn(
                name: "ValorCbs",
                table: "PedidosVenda");

            migrationBuilder.DropColumn(
                name: "ValorDesconto",
                table: "PedidosVenda");

            migrationBuilder.DropColumn(
                name: "ValorIbs",
                table: "PedidosVenda");

            migrationBuilder.DropColumn(
                name: "ValorOutrasDespesas",
                table: "PedidosVenda");

            migrationBuilder.DropColumn(
                name: "ValorSeguro",
                table: "PedidosVenda");

            migrationBuilder.DropColumn(
                name: "VeiculoProprio",
                table: "PedidosVenda");

            migrationBuilder.DropColumn(
                name: "DataSaida",
                table: "NotasFiscais");

            migrationBuilder.DropColumn(
                name: "HoraSaida",
                table: "NotasFiscais");

            migrationBuilder.DropColumn(
                name: "IdTransportadora",
                table: "NotasFiscais");

            migrationBuilder.DropColumn(
                name: "ValorCbs",
                table: "NotasFiscais");

            migrationBuilder.DropColumn(
                name: "ValorIbs",
                table: "NotasFiscais");

            migrationBuilder.AlterColumn<decimal>(
                name: "IcmsValor",
                table: "NotasFiscais",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");
        }
    }
}
