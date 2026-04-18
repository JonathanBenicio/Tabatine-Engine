using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tabatine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSyncLockFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "acquired_at",
                table: "sync_locks",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "owner",
                table: "sync_locks",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "acquired_at",
                table: "sync_locks");

            migrationBuilder.DropColumn(
                name: "owner",
                table: "sync_locks");
        }
    }
}
