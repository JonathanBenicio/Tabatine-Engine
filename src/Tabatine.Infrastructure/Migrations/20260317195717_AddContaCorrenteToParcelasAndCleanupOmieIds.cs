using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tabatine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddContaCorrenteToParcelasAndCleanupOmieIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CodigoContaCorrente",
                table: "PedidosVenda");

            migrationBuilder.DropColumn(
                name: "CodigoVendedor",
                table: "PedidosVenda");

            migrationBuilder.DropColumn(
                name: "CodigoContaCorrente",
                table: "NotaFiscalTitulos");

            migrationBuilder.AddColumn<Guid>(
                name: "ContaCorrenteId",
                table: "PedidoParcelas",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PedidoParcelas_ContaCorrenteId",
                table: "PedidoParcelas",
                column: "ContaCorrenteId");

            migrationBuilder.AddForeignKey(
                name: "FK_PedidoParcelas_ContasCorrente_ContaCorrenteId",
                table: "PedidoParcelas",
                column: "ContaCorrenteId",
                principalTable: "ContasCorrente",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PedidoParcelas_ContasCorrente_ContaCorrenteId",
                table: "PedidoParcelas");

            migrationBuilder.DropIndex(
                name: "IX_PedidoParcelas_ContaCorrenteId",
                table: "PedidoParcelas");

            migrationBuilder.DropColumn(
                name: "ContaCorrenteId",
                table: "PedidoParcelas");

            migrationBuilder.AddColumn<long>(
                name: "CodigoContaCorrente",
                table: "PedidosVenda",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CodigoVendedor",
                table: "PedidosVenda",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CodigoContaCorrente",
                table: "NotaFiscalTitulos",
                type: "bigint",
                nullable: true);
        }
    }
}
