using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tabatine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RefineRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "MeioPagamentoId",
                table: "PedidoParcelas",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TabelaPrecoId",
                table: "ItensPedido",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProdutoCaracteristicas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProdutoId = table.Column<Guid>(type: "uuid", nullable: false),
                    CaracteristicaId = table.Column<Guid>(type: "uuid", nullable: false),
                    CaracteristicaValorId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProdutoCaracteristicas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProdutoCaracteristicas_CaracteristicaValores_Caracteristica~",
                        column: x => x.CaracteristicaValorId,
                        principalTable: "CaracteristicaValores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProdutoCaracteristicas_Caracteristicas_CaracteristicaId",
                        column: x => x.CaracteristicaId,
                        principalTable: "Caracteristicas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProdutoCaracteristicas_Produtos_ProdutoId",
                        column: x => x.ProdutoId,
                        principalTable: "Produtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PedidoParcelas_MeioPagamentoId",
                table: "PedidoParcelas",
                column: "MeioPagamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_ItensPedido_TabelaPrecoId",
                table: "ItensPedido",
                column: "TabelaPrecoId");

            migrationBuilder.CreateIndex(
                name: "IX_ProdutoCaracteristicas_CaracteristicaId",
                table: "ProdutoCaracteristicas",
                column: "CaracteristicaId");

            migrationBuilder.CreateIndex(
                name: "IX_ProdutoCaracteristicas_CaracteristicaValorId",
                table: "ProdutoCaracteristicas",
                column: "CaracteristicaValorId");

            migrationBuilder.CreateIndex(
                name: "IX_ProdutoCaracteristicas_ProdutoId",
                table: "ProdutoCaracteristicas",
                column: "ProdutoId");

            migrationBuilder.AddForeignKey(
                name: "FK_ItensPedido_TabelasPreco_TabelaPrecoId",
                table: "ItensPedido",
                column: "TabelaPrecoId",
                principalTable: "TabelasPreco",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PedidoParcelas_MeiosPagamento_MeioPagamentoId",
                table: "PedidoParcelas",
                column: "MeioPagamentoId",
                principalTable: "MeiosPagamento",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ItensPedido_TabelasPreco_TabelaPrecoId",
                table: "ItensPedido");

            migrationBuilder.DropForeignKey(
                name: "FK_PedidoParcelas_MeiosPagamento_MeioPagamentoId",
                table: "PedidoParcelas");

            migrationBuilder.DropTable(
                name: "ProdutoCaracteristicas");

            migrationBuilder.DropIndex(
                name: "IX_PedidoParcelas_MeioPagamentoId",
                table: "PedidoParcelas");

            migrationBuilder.DropIndex(
                name: "IX_ItensPedido_TabelaPrecoId",
                table: "ItensPedido");

            migrationBuilder.DropColumn(
                name: "MeioPagamentoId",
                table: "PedidoParcelas");

            migrationBuilder.DropColumn(
                name: "TabelaPrecoId",
                table: "ItensPedido");
        }
    }
}
