using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tabatine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSellerAndBankAccountRelations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "CodigoContaCorrente",
                table: "PedidosVenda",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ContaCorrenteId",
                table: "PedidosVenda",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "VendedorId",
                table: "PedidosVenda",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CodigoContaCorrente",
                table: "NotaFiscalTitulos",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ContaCorrenteId",
                table: "NotaFiscalTitulos",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PedidosVenda_ContaCorrenteId",
                table: "PedidosVenda",
                column: "ContaCorrenteId");

            migrationBuilder.CreateIndex(
                name: "IX_PedidosVenda_VendedorId",
                table: "PedidosVenda",
                column: "VendedorId");

            migrationBuilder.CreateIndex(
                name: "IX_NotaFiscalTitulos_ContaCorrenteId",
                table: "NotaFiscalTitulos",
                column: "ContaCorrenteId");

            migrationBuilder.AddForeignKey(
                name: "FK_NotaFiscalTitulos_ContasCorrente_ContaCorrenteId",
                table: "NotaFiscalTitulos",
                column: "ContaCorrenteId",
                principalTable: "ContasCorrente",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PedidosVenda_ContasCorrente_ContaCorrenteId",
                table: "PedidosVenda",
                column: "ContaCorrenteId",
                principalTable: "ContasCorrente",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PedidosVenda_Vendedores_VendedorId",
                table: "PedidosVenda",
                column: "VendedorId",
                principalTable: "Vendedores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NotaFiscalTitulos_ContasCorrente_ContaCorrenteId",
                table: "NotaFiscalTitulos");

            migrationBuilder.DropForeignKey(
                name: "FK_PedidosVenda_ContasCorrente_ContaCorrenteId",
                table: "PedidosVenda");

            migrationBuilder.DropForeignKey(
                name: "FK_PedidosVenda_Vendedores_VendedorId",
                table: "PedidosVenda");

            migrationBuilder.DropIndex(
                name: "IX_PedidosVenda_ContaCorrenteId",
                table: "PedidosVenda");

            migrationBuilder.DropIndex(
                name: "IX_PedidosVenda_VendedorId",
                table: "PedidosVenda");

            migrationBuilder.DropIndex(
                name: "IX_NotaFiscalTitulos_ContaCorrenteId",
                table: "NotaFiscalTitulos");

            migrationBuilder.DropColumn(
                name: "CodigoContaCorrente",
                table: "PedidosVenda");

            migrationBuilder.DropColumn(
                name: "ContaCorrenteId",
                table: "PedidosVenda");

            migrationBuilder.DropColumn(
                name: "VendedorId",
                table: "PedidosVenda");

            migrationBuilder.DropColumn(
                name: "CodigoContaCorrente",
                table: "NotaFiscalTitulos");

            migrationBuilder.DropColumn(
                name: "ContaCorrenteId",
                table: "NotaFiscalTitulos");
        }
    }
}
