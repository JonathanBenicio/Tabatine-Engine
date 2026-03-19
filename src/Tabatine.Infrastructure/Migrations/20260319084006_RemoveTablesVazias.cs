using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tabatine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTablesVazias : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProdutoCaracteristicas");

            migrationBuilder.DropTable(
                name: "CaracteristicaValores");

            migrationBuilder.DropTable(
                name: "Caracteristicas");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Caracteristicas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OmieId = table.Column<long>(type: "bigint", nullable: false),
                    OmieUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Caracteristicas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CaracteristicaValores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CaracteristicaId = table.Column<Guid>(type: "uuid", nullable: false),
                    OmieIdConteudo = table.Column<long>(type: "bigint", nullable: true),
                    Valor = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaracteristicaValores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CaracteristicaValores_Caracteristicas_CaracteristicaId",
                        column: x => x.CaracteristicaId,
                        principalTable: "Caracteristicas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProdutoCaracteristicas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CaracteristicaId = table.Column<Guid>(type: "uuid", nullable: false),
                    CaracteristicaValorId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProdutoId = table.Column<Guid>(type: "uuid", nullable: false)
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
                name: "IX_Caracteristicas_OmieId",
                table: "Caracteristicas",
                column: "OmieId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CaracteristicaValores_CaracteristicaId",
                table: "CaracteristicaValores",
                column: "CaracteristicaId");

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
        }
    }
}
