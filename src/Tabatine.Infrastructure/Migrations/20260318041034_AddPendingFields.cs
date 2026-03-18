using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tabatine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPendingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DO $$ BEGIN
                    ALTER TABLE ""PedidosVenda"" ADD COLUMN ""ComissaoVendedor"" numeric(10,2) NOT NULL DEFAULT 0.0;
                EXCEPTION WHEN duplicate_column THEN NULL; END $$;

                DO $$ BEGIN
                    ALTER TABLE ""NotasFiscais"" ADD COLUMN ""ImportadoApi"" boolean NOT NULL DEFAULT false;
                EXCEPTION WHEN duplicate_column THEN NULL; END $$;

                DO $$ BEGIN
                    ALTER TABLE ""NotasFiscais"" ADD COLUMN ""NaturezaOperacao"" character varying(200);
                EXCEPTION WHEN duplicate_column THEN NULL; END $$;

                DO $$ BEGIN
                    ALTER TABLE ""NotasFiscais"" ADD COLUMN ""Serie"" character varying(20);
                EXCEPTION WHEN duplicate_column THEN NULL; END $$;

                DO $$ BEGIN
                    ALTER TABLE ""ItensPedido"" ADD COLUMN ""UnidadeMedida"" character varying(10);
                EXCEPTION WHEN duplicate_column THEN NULL; END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ComissaoVendedor",
                table: "PedidosVenda");

            migrationBuilder.DropColumn(
                name: "ImportadoApi",
                table: "NotasFiscais");

            migrationBuilder.DropColumn(
                name: "NaturezaOperacao",
                table: "NotasFiscais");

            migrationBuilder.DropColumn(
                name: "Serie",
                table: "NotasFiscais");

            migrationBuilder.DropColumn(
                name: "UnidadeMedida",
                table: "ItensPedido");
        }
    }
}
