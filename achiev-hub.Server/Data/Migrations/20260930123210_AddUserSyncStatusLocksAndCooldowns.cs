using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace achiev_hub.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserSyncStatusLocksAndCooldowns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_auto_enqueue_at",
                table: "user_sync_status",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "locked_until",
                table: "user_sync_status",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "manual_enqueue_count",
                table: "user_sync_status",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateOnly>(
                name: "manual_enqueue_date",
                table: "user_sync_status",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "last_auto_enqueue_at",
                table: "user_sync_status");

            migrationBuilder.DropColumn(
                name: "locked_until",
                table: "user_sync_status");

            migrationBuilder.DropColumn(
                name: "manual_enqueue_count",
                table: "user_sync_status");

            migrationBuilder.DropColumn(
                name: "manual_enqueue_date",
                table: "user_sync_status");
        }
    }
}
