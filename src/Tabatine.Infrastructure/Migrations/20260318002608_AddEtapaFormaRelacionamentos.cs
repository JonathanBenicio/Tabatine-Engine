using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tabatine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEtapaFormaRelacionamentos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "EtapaFaturamentoId",
                table: "PedidosVenda",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FormaPagamentoId",
                table: "PedidosVenda",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BancoId",
                table: "ContasCorrente",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PedidosVenda_EtapaFaturamentoId",
                table: "PedidosVenda",
                column: "EtapaFaturamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_PedidosVenda_FormaPagamentoId",
                table: "PedidosVenda",
                column: "FormaPagamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_ContasCorrente_BancoId",
                table: "ContasCorrente",
                column: "BancoId");

            migrationBuilder.AddForeignKey(
                name: "FK_ContasCorrente_Bancos_BancoId",
                table: "ContasCorrente",
                column: "BancoId",
                principalTable: "Bancos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PedidosVenda_EtapasFaturamento_EtapaFaturamentoId",
                table: "PedidosVenda",
                column: "EtapaFaturamentoId",
                principalTable: "EtapasFaturamento",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PedidosVenda_FormasPagamento_FormaPagamentoId",
                table: "PedidosVenda",
                column: "FormaPagamentoId",
                principalTable: "FormasPagamento",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContasCorrente_Bancos_BancoId",
                table: "ContasCorrente");

            migrationBuilder.DropForeignKey(
                name: "FK_PedidosVenda_EtapasFaturamento_EtapaFaturamentoId",
                table: "PedidosVenda");

            migrationBuilder.DropForeignKey(
                name: "FK_PedidosVenda_FormasPagamento_FormaPagamentoId",
                table: "PedidosVenda");

            migrationBuilder.DropIndex(
                name: "IX_PedidosVenda_EtapaFaturamentoId",
                table: "PedidosVenda");

            migrationBuilder.DropIndex(
                name: "IX_PedidosVenda_FormaPagamentoId",
                table: "PedidosVenda");

            migrationBuilder.DropIndex(
                name: "IX_ContasCorrente_BancoId",
                table: "ContasCorrente");

            migrationBuilder.DropColumn(
                name: "EtapaFaturamentoId",
                table: "PedidosVenda");

            migrationBuilder.DropColumn(
                name: "FormaPagamentoId",
                table: "PedidosVenda");

            migrationBuilder.DropColumn(
                name: "BancoId",
                table: "ContasCorrente");
        }
    }
}
