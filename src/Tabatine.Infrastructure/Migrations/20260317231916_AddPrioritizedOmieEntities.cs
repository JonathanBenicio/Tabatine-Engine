using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tabatine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPrioritizedOmieEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Bancos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CodigoBanco = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    CodigoIspb = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Tipo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    OmieId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OmieUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bancos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EtapasFaturamento",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Codigo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DescricaoPadrao = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Inativa = table.Column<bool>(type: "boolean", nullable: false),
                    CodigoOperacao = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DescricaoOperacao = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OmieId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OmieUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EtapasFaturamento", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FormasPagamento",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Codigo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    QuantidadeParcelas = table.Column<int>(type: "integer", nullable: false),
                    DiasParcelas = table.Column<int>(type: "integer", nullable: true),
                    ListaParcelas = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    OmieId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OmieUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormasPagamento", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bancos_CodigoBanco",
                table: "Bancos",
                column: "CodigoBanco",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EtapasFaturamento_CodigoOperacao_Codigo",
                table: "EtapasFaturamento",
                columns: new[] { "CodigoOperacao", "Codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FormasPagamento_Codigo",
                table: "FormasPagamento",
                column: "Codigo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Bancos");

            migrationBuilder.DropTable(
                name: "EtapasFaturamento");

            migrationBuilder.DropTable(
                name: "FormasPagamento");
        }
    }
}
