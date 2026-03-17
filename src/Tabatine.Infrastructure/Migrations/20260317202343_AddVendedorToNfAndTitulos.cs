using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tabatine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVendedorToNfAndTitulos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ContaCorrenteId",
                table: "NotasFiscais",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "VendedorId",
                table: "NotasFiscais",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "VendedorId",
                table: "NotaFiscalTitulos",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotasFiscais_ContaCorrenteId",
                table: "NotasFiscais",
                column: "ContaCorrenteId");

            migrationBuilder.CreateIndex(
                name: "IX_NotasFiscais_VendedorId",
                table: "NotasFiscais",
                column: "VendedorId");

            migrationBuilder.CreateIndex(
                name: "IX_NotaFiscalTitulos_VendedorId",
                table: "NotaFiscalTitulos",
                column: "VendedorId");

            migrationBuilder.AddForeignKey(
                name: "FK_NotaFiscalTitulos_Vendedores_VendedorId",
                table: "NotaFiscalTitulos",
                column: "VendedorId",
                principalTable: "Vendedores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_NotasFiscais_ContasCorrente_ContaCorrenteId",
                table: "NotasFiscais",
                column: "ContaCorrenteId",
                principalTable: "ContasCorrente",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_NotasFiscais_Vendedores_VendedorId",
                table: "NotasFiscais",
                column: "VendedorId",
                principalTable: "Vendedores",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NotaFiscalTitulos_Vendedores_VendedorId",
                table: "NotaFiscalTitulos");

            migrationBuilder.DropForeignKey(
                name: "FK_NotasFiscais_ContasCorrente_ContaCorrenteId",
                table: "NotasFiscais");

            migrationBuilder.DropForeignKey(
                name: "FK_NotasFiscais_Vendedores_VendedorId",
                table: "NotasFiscais");

            migrationBuilder.DropIndex(
                name: "IX_NotasFiscais_ContaCorrenteId",
                table: "NotasFiscais");

            migrationBuilder.DropIndex(
                name: "IX_NotasFiscais_VendedorId",
                table: "NotasFiscais");

            migrationBuilder.DropIndex(
                name: "IX_NotaFiscalTitulos_VendedorId",
                table: "NotaFiscalTitulos");

            migrationBuilder.DropColumn(
                name: "ContaCorrenteId",
                table: "NotasFiscais");

            migrationBuilder.DropColumn(
                name: "VendedorId",
                table: "NotasFiscais");

            migrationBuilder.DropColumn(
                name: "VendedorId",
                table: "NotaFiscalTitulos");
        }
    }
}
