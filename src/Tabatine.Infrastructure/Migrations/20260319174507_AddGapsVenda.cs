using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tabatine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGapsVenda : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "QuantidadeParcelas",
                table: "PedidosVenda",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Categoria",
                table: "PedidoParcelas",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Nsu",
                table: "PedidoParcelas",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AliqCbs",
                table: "ItensPedido",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "AliqIbs",
                table: "ItensPedido",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "BaseIbsCbs",
                table: "ItensPedido",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorCbs",
                table: "ItensPedido",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorIbs",
                table: "ItensPedido",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "QuantidadeParcelas",
                table: "PedidosVenda");

            migrationBuilder.DropColumn(
                name: "Categoria",
                table: "PedidoParcelas");

            migrationBuilder.DropColumn(
                name: "Nsu",
                table: "PedidoParcelas");

            migrationBuilder.DropColumn(
                name: "AliqCbs",
                table: "ItensPedido");

            migrationBuilder.DropColumn(
                name: "AliqIbs",
                table: "ItensPedido");

            migrationBuilder.DropColumn(
                name: "BaseIbsCbs",
                table: "ItensPedido");

            migrationBuilder.DropColumn(
                name: "ValorCbs",
                table: "ItensPedido");

            migrationBuilder.DropColumn(
                name: "ValorIbs",
                table: "ItensPedido");
        }
    }
}
