using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tabatine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ExpandPedidoData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "BaseCalculoIcms",
                table: "PedidosVenda",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PesoBruto",
                table: "PedidosVenda",
                type: "numeric(18,3)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PesoLiquido",
                table: "PedidosVenda",
                type: "numeric(18,3)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "PrevisaoEntrega",
                table: "PedidosVenda",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorCofins",
                table: "PedidosVenda",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorIcms",
                table: "PedidosVenda",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorIpi",
                table: "PedidosVenda",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorMercadorias",
                table: "PedidosVenda",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorPis",
                table: "PedidosVenda",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PesoBruto",
                table: "ItensPedido",
                type: "numeric(18,3)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PesoLiquido",
                table: "ItensPedido",
                type: "numeric(18,3)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BaseCalculoIcms",
                table: "PedidosVenda");

            migrationBuilder.DropColumn(
                name: "PesoBruto",
                table: "PedidosVenda");

            migrationBuilder.DropColumn(
                name: "PesoLiquido",
                table: "PedidosVenda");

            migrationBuilder.DropColumn(
                name: "PrevisaoEntrega",
                table: "PedidosVenda");

            migrationBuilder.DropColumn(
                name: "ValorCofins",
                table: "PedidosVenda");

            migrationBuilder.DropColumn(
                name: "ValorIcms",
                table: "PedidosVenda");

            migrationBuilder.DropColumn(
                name: "ValorIpi",
                table: "PedidosVenda");

            migrationBuilder.DropColumn(
                name: "ValorMercadorias",
                table: "PedidosVenda");

            migrationBuilder.DropColumn(
                name: "ValorPis",
                table: "PedidosVenda");

            migrationBuilder.DropColumn(
                name: "PesoBruto",
                table: "ItensPedido");

            migrationBuilder.DropColumn(
                name: "PesoLiquido",
                table: "ItensPedido");
        }
    }
}
