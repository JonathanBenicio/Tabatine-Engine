using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tabatine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEstoqueEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "estoque_locais",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    codigo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    descricao = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    tipo = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: true),
                    padrao = table.Column<bool>(type: "boolean", nullable: false),
                    inativo = table.Column<bool>(type: "boolean", nullable: false),
                    omie_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    omie_updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_estoque_locais", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "estoque_produtos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    produto_id = table.Column<Guid>(type: "uuid", nullable: false),
                    local_estoque_id = table.Column<Guid>(type: "uuid", nullable: false),
                    saldo = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    reservado = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    pendente = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    fisico = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    cmc = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    estoque_minimo = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_estoque_produtos", x => x.id);
                    table.ForeignKey(
                        name: "fk_estoque_produtos_estoque_locais_local_estoque_id",
                        column: x => x.local_estoque_id,
                        principalTable: "estoque_locais",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_estoque_produtos_produtos_produto_id",
                        column: x => x.produto_id,
                        principalTable: "produtos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_estoque_locais_omie_id",
                table: "estoque_locais",
                column: "omie_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_estoque_produtos_local_estoque_id",
                table: "estoque_produtos",
                column: "local_estoque_id");

            migrationBuilder.CreateIndex(
                name: "ix_estoque_produtos_produto_id_local_estoque_id",
                table: "estoque_produtos",
                columns: new[] { "produto_id", "local_estoque_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "estoque_produtos");

            migrationBuilder.DropTable(
                name: "estoque_locais");
        }
    }
}
