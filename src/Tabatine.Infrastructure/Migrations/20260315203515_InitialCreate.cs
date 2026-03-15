using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tabatine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Clientes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RazaoSocial = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    NomeFantasia = table.Column<string>(type: "text", nullable: false),
                    CnpjCpf = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "text", nullable: true),
                    Telefone = table.Column<string>(type: "text", nullable: true),
                    InscricaoEstadual = table.Column<string>(type: "text", nullable: true),
                    Cep = table.Column<string>(type: "text", nullable: true),
                    Estado = table.Column<string>(type: "text", nullable: true),
                    Cidade = table.Column<string>(type: "text", nullable: true),
                    OmieId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OmieUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clientes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Produtos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CodigoProduto = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Ncm = table.Column<string>(type: "text", nullable: false),
                    Ean = table.Column<string>(type: "text", nullable: true),
                    PrecoUnitario = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    OmieId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OmieUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Produtos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PedidosVenda",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NumeroPedido = table.Column<string>(type: "text", nullable: false),
                    Etapa = table.Column<string>(type: "text", nullable: false),
                    ValorTotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DataPrevisao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ClienteId = table.Column<Guid>(type: "uuid", nullable: false),
                    OmieId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OmieUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PedidosVenda", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PedidosVenda_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ItensPedido",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PedidoVendaId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProdutoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantidade = table.Column<int>(type: "integer", nullable: false),
                    ValorUnitario = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ValorTotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    OmieId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OmieUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItensPedido", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItensPedido_PedidosVenda_PedidoVendaId",
                        column: x => x.PedidoVendaId,
                        principalTable: "PedidosVenda",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ItensPedido_Produtos_ProdutoId",
                        column: x => x.ProdutoId,
                        principalTable: "Produtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NotasFiscais",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NumeroNf = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ChaveAcesso = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    DataEmissao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ValorTotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    PedidoVendaId = table.Column<Guid>(type: "uuid", nullable: true),
                    ClienteId = table.Column<Guid>(type: "uuid", nullable: false),
                    OmieId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OmieUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotasFiscais", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotasFiscais_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NotasFiscais_PedidosVenda_PedidoVendaId",
                        column: x => x.PedidoVendaId,
                        principalTable: "PedidosVenda",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_OmieId",
                table: "Clientes",
                column: "OmieId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItensPedido_PedidoVendaId",
                table: "ItensPedido",
                column: "PedidoVendaId");

            migrationBuilder.CreateIndex(
                name: "IX_ItensPedido_ProdutoId",
                table: "ItensPedido",
                column: "ProdutoId");

            migrationBuilder.CreateIndex(
                name: "IX_NotasFiscais_ClienteId",
                table: "NotasFiscais",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_NotasFiscais_OmieId",
                table: "NotasFiscais",
                column: "OmieId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotasFiscais_PedidoVendaId",
                table: "NotasFiscais",
                column: "PedidoVendaId");

            migrationBuilder.CreateIndex(
                name: "IX_PedidosVenda_ClienteId",
                table: "PedidosVenda",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_PedidosVenda_OmieId",
                table: "PedidosVenda",
                column: "OmieId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Produtos_OmieId",
                table: "Produtos",
                column: "OmieId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ItensPedido");

            migrationBuilder.DropTable(
                name: "NotasFiscais");

            migrationBuilder.DropTable(
                name: "Produtos");

            migrationBuilder.DropTable(
                name: "PedidosVenda");

            migrationBuilder.DropTable(
                name: "Clientes");
        }
    }
}
