using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tabatine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTabelasPrecos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ItensPedido_TabelasPreco_TabelaPrecoId",
                table: "ItensPedido");

            migrationBuilder.DropTable(
                name: "TabelaPrecoItens");

            migrationBuilder.DropTable(
                name: "TabelasPreco");

            migrationBuilder.DropIndex(
                name: "IX_ItensPedido_TabelaPrecoId",
                table: "ItensPedido");

            migrationBuilder.DropColumn(
                name: "TabelaPrecoId",
                table: "ItensPedido");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TabelaPrecoId",
                table: "ItensPedido",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TabelasPreco",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Ativa = table.Column<bool>(type: "boolean", nullable: false),
                    Codigo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    OmieId = table.Column<long>(type: "bigint", nullable: false),
                    OmieUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TabelasPreco", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TabelaPrecoItens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProdutoId = table.Column<Guid>(type: "uuid", nullable: false),
                    TabelaPrecoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Valor = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TabelaPrecoItens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TabelaPrecoItens_Produtos_ProdutoId",
                        column: x => x.ProdutoId,
                        principalTable: "Produtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TabelaPrecoItens_TabelasPreco_TabelaPrecoId",
                        column: x => x.TabelaPrecoId,
                        principalTable: "TabelasPreco",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ItensPedido_TabelaPrecoId",
                table: "ItensPedido",
                column: "TabelaPrecoId");

            migrationBuilder.CreateIndex(
                name: "IX_TabelaPrecoItens_ProdutoId",
                table: "TabelaPrecoItens",
                column: "ProdutoId");

            migrationBuilder.CreateIndex(
                name: "IX_TabelaPrecoItens_TabelaPrecoId",
                table: "TabelaPrecoItens",
                column: "TabelaPrecoId");

            migrationBuilder.CreateIndex(
                name: "IX_TabelasPreco_OmieId",
                table: "TabelasPreco",
                column: "OmieId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ItensPedido_TabelasPreco_TabelaPrecoId",
                table: "ItensPedido",
                column: "TabelaPrecoId",
                principalTable: "TabelasPreco",
                principalColumn: "Id");
        }
    }
}
