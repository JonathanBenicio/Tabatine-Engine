using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tabatine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPedidoVendaCamposFaltantes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Autorizado",
                table: "PedidosVenda",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Cancelado",
                table: "PedidosVenda",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "CodigoParcela",
                table: "PedidosVenda",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Contato",
                table: "PedidosVenda",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DataInclusao",
                table: "PedidosVenda",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Denegado",
                table: "PedidosVenda",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Devolvido",
                table: "PedidosVenda",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "UsuarioAlteracao",
                table: "PedidosVenda",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Autorizado",
                table: "PedidosVenda");

            migrationBuilder.DropColumn(
                name: "Cancelado",
                table: "PedidosVenda");

            migrationBuilder.DropColumn(
                name: "CodigoParcela",
                table: "PedidosVenda");

            migrationBuilder.DropColumn(
                name: "Contato",
                table: "PedidosVenda");

            migrationBuilder.DropColumn(
                name: "DataInclusao",
                table: "PedidosVenda");

            migrationBuilder.DropColumn(
                name: "Denegado",
                table: "PedidosVenda");

            migrationBuilder.DropColumn(
                name: "Devolvido",
                table: "PedidosVenda");

            migrationBuilder.DropColumn(
                name: "UsuarioAlteracao",
                table: "PedidosVenda");
        }
    }
}
