using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tabatine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ExpandSyncCapabilities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ItensNotaFiscal",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NotaFiscalId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProdutoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantidade = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    ValorUnitario = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ValorTotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Cfop = table.Column<string>(type: "text", nullable: true),
                    Ncm = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItensNotaFiscal", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItensNotaFiscal_NotasFiscais_NotaFiscalId",
                        column: x => x.NotaFiscalId,
                        principalTable: "NotasFiscais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ItensNotaFiscal_Produtos_ProdutoId",
                        column: x => x.ProdutoId,
                        principalTable: "Produtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NotaFiscalTitulos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NotaFiscalId = table.Column<Guid>(type: "uuid", nullable: false),
                    NumeroParcela = table.Column<int>(type: "integer", nullable: false),
                    Valor = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DataVencimento = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OmieIdTitulo = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotaFiscalTitulos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotaFiscalTitulos_NotasFiscais_NotaFiscalId",
                        column: x => x.NotaFiscalId,
                        principalTable: "NotasFiscais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PedidoParcelas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PedidoVendaId = table.Column<Guid>(type: "uuid", nullable: false),
                    NumeroParcela = table.Column<int>(type: "integer", nullable: false),
                    Valor = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DataVencimento = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Percentual = table.Column<decimal>(type: "numeric(5,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PedidoParcelas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PedidoParcelas_PedidosVenda_PedidoVendaId",
                        column: x => x.PedidoVendaId,
                        principalTable: "PedidosVenda",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ItensNotaFiscal_NotaFiscalId",
                table: "ItensNotaFiscal",
                column: "NotaFiscalId");

            migrationBuilder.CreateIndex(
                name: "IX_ItensNotaFiscal_ProdutoId",
                table: "ItensNotaFiscal",
                column: "ProdutoId");

            migrationBuilder.CreateIndex(
                name: "IX_NotaFiscalTitulos_NotaFiscalId",
                table: "NotaFiscalTitulos",
                column: "NotaFiscalId");

            migrationBuilder.CreateIndex(
                name: "IX_PedidoParcelas_PedidoVendaId",
                table: "PedidoParcelas",
                column: "PedidoVendaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ItensNotaFiscal");

            migrationBuilder.DropTable(
                name: "NotaFiscalTitulos");

            migrationBuilder.DropTable(
                name: "PedidoParcelas");
        }
    }
}
