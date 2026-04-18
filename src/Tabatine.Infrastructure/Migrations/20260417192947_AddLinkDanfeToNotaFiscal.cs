using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tabatine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLinkDanfeToNotaFiscal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "link_danfe",
                table: "notas_fiscais",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "link_danfe",
                table: "notas_fiscais");
        }
    }
}
