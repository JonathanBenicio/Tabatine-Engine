using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tabatine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTitulosFinanceiros : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "titulos_pagar",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    numero_documento = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    numero_parcela = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    numero_pedido = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    data_emissao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    data_vencimento = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    data_baixa = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    valor_documento = table.Column<decimal>(type: "numeric(15,2)", precision: 15, scale: 2, nullable: false),
                    valor_pago = table.Column<decimal>(type: "numeric(15,2)", precision: 15, scale: 2, nullable: false),
                    valor_saldo = table.Column<decimal>(type: "numeric(15,2)", precision: 15, scale: 2, nullable: false),
                    status_titulo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    codigo_categoria = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    observacao = table.Column<string>(type: "text", nullable: true),
                    cliente_id = table.Column<Guid>(type: "uuid", nullable: true),
                    conta_corrente_id = table.Column<Guid>(type: "uuid", nullable: true),
                    omie_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    omie_updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_titulos_pagar", x => x.id);
                    table.ForeignKey(
                        name: "fk_titulos_pagar_clientes_cliente_id",
                        column: x => x.cliente_id,
                        principalTable: "clientes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_titulos_pagar_contas_corrente_conta_corrente_id",
                        column: x => x.conta_corrente_id,
                        principalTable: "contas_corrente",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "titulos_receber",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    numero_documento = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    numero_parcela = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    numero_pedido = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    data_emissao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    data_vencimento = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    data_previsao = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    data_baixa = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    valor_documento = table.Column<decimal>(type: "numeric(15,2)", precision: 15, scale: 2, nullable: false),
                    valor_recebido = table.Column<decimal>(type: "numeric(15,2)", precision: 15, scale: 2, nullable: false),
                    valor_saldo = table.Column<decimal>(type: "numeric(15,2)", precision: 15, scale: 2, nullable: false),
                    status_titulo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    codigo_categoria = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    observacao = table.Column<string>(type: "text", nullable: true),
                    cliente_id = table.Column<Guid>(type: "uuid", nullable: true),
                    vendedor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    conta_corrente_id = table.Column<Guid>(type: "uuid", nullable: true),
                    omie_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    omie_updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_titulos_receber", x => x.id);
                    table.ForeignKey(
                        name: "fk_titulos_receber_clientes_cliente_id",
                        column: x => x.cliente_id,
                        principalTable: "clientes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_titulos_receber_contas_corrente_conta_corrente_id",
                        column: x => x.conta_corrente_id,
                        principalTable: "contas_corrente",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_titulos_receber_vendedores_vendedor_id",
                        column: x => x.vendedor_id,
                        principalTable: "vendedores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_titulos_pagar_cliente_id",
                table: "titulos_pagar",
                column: "cliente_id");

            migrationBuilder.CreateIndex(
                name: "ix_titulos_pagar_conta_corrente_id",
                table: "titulos_pagar",
                column: "conta_corrente_id");

            migrationBuilder.CreateIndex(
                name: "ix_titulos_pagar_data_vencimento",
                table: "titulos_pagar",
                column: "data_vencimento");

            migrationBuilder.CreateIndex(
                name: "ix_titulos_pagar_omie_id",
                table: "titulos_pagar",
                column: "omie_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_titulos_pagar_status_titulo",
                table: "titulos_pagar",
                column: "status_titulo");

            migrationBuilder.CreateIndex(
                name: "ix_titulos_receber_cliente_id",
                table: "titulos_receber",
                column: "cliente_id");

            migrationBuilder.CreateIndex(
                name: "ix_titulos_receber_conta_corrente_id",
                table: "titulos_receber",
                column: "conta_corrente_id");

            migrationBuilder.CreateIndex(
                name: "ix_titulos_receber_data_vencimento",
                table: "titulos_receber",
                column: "data_vencimento");

            migrationBuilder.CreateIndex(
                name: "ix_titulos_receber_omie_id",
                table: "titulos_receber",
                column: "omie_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_titulos_receber_status_titulo",
                table: "titulos_receber",
                column: "status_titulo");

            migrationBuilder.CreateIndex(
                name: "ix_titulos_receber_vendedor_id",
                table: "titulos_receber",
                column: "vendedor_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "titulos_pagar");

            migrationBuilder.DropTable(
                name: "titulos_receber");
        }
    }
}
