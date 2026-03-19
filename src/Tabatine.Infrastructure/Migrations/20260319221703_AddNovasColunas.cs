using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tabatine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNovasColunas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AliqCbs",
                table: "ItensNotaFiscal",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "AliqIbs",
                table: "ItensNotaFiscal",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "BaseIbsCbs",
                table: "ItensNotaFiscal",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorCbs",
                table: "ItensNotaFiscal",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorIbs",
                table: "ItensNotaFiscal",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AliqCbs",
                table: "ItensNotaFiscal");

            migrationBuilder.DropColumn(
                name: "AliqIbs",
                table: "ItensNotaFiscal");

            migrationBuilder.DropColumn(
                name: "BaseIbsCbs",
                table: "ItensNotaFiscal");

            migrationBuilder.DropColumn(
                name: "ValorCbs",
                table: "ItensNotaFiscal");

            migrationBuilder.DropColumn(
                name: "ValorIbs",
                table: "ItensNotaFiscal");
        }
    }
}
