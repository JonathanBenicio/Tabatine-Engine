using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tabatine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateWebhookEventDLQ : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "error_message",
                table: "webhook_events",
                newName: "last_error_detail");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "webhook_events",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<DateTime>(
                name: "last_attempt_at",
                table: "webhook_events",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "max_retries",
                table: "webhook_events",
                type: "integer",
                nullable: false,
                defaultValue: 5);

            migrationBuilder.AddColumn<DateTime>(
                name: "next_retry_at",
                table: "webhook_events",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "retry_count",
                table: "webhook_events",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "ix_webhook_events_event",
                table: "webhook_events",
                column: "event");

            migrationBuilder.CreateIndex(
                name: "ix_webhook_events_message_id",
                table: "webhook_events",
                column: "message_id");

            migrationBuilder.CreateIndex(
                name: "ix_webhook_events_next_retry_at",
                table: "webhook_events",
                column: "next_retry_at",
                filter: "\"status\" = 'Failed'");

            migrationBuilder.CreateIndex(
                name: "ix_webhook_events_status",
                table: "webhook_events",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_webhook_events_event",
                table: "webhook_events");

            migrationBuilder.DropIndex(
                name: "ix_webhook_events_message_id",
                table: "webhook_events");

            migrationBuilder.DropIndex(
                name: "ix_webhook_events_next_retry_at",
                table: "webhook_events");

            migrationBuilder.DropIndex(
                name: "ix_webhook_events_status",
                table: "webhook_events");

            migrationBuilder.DropColumn(
                name: "last_attempt_at",
                table: "webhook_events");

            migrationBuilder.DropColumn(
                name: "max_retries",
                table: "webhook_events");

            migrationBuilder.DropColumn(
                name: "next_retry_at",
                table: "webhook_events");

            migrationBuilder.DropColumn(
                name: "retry_count",
                table: "webhook_events");

            migrationBuilder.RenameColumn(
                name: "last_error_detail",
                table: "webhook_events",
                newName: "error_message");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "webhook_events",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "now()");
        }
    }
}
