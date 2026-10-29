using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace achiev_hub.Server.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGoalStatusAndTimestamps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_goals_UserId",
                table: "goals");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CompletedAt",
                table: "goals",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "goals",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "goals",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_goals_UserId_GameId",
                table: "goals",
                columns: new[] { "UserId", "GameId" },
                unique: true,
                filter: "\"Status\" = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_goals_UserId_GameId",
                table: "goals");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "goals");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "goals");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "goals");

            migrationBuilder.CreateIndex(
                name: "IX_goals_UserId",
                table: "goals",
                column: "UserId");
        }
    }
}
