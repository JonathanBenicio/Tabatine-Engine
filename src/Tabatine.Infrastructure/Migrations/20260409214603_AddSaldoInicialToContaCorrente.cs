using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tabatine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSaldoInicialToContaCorrente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "saldo_inicial",
                table: "contas_corrente",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "saldo_inicial",
                table: "contas_corrente");
        }
    }
}
