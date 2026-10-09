using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace achiev_hub.Server.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SyncSteamWorkerUserGameColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Present in the model snapshot historically but never added by a prior migration Up().
            migrationBuilder.Sql(
                """
                ALTER TABLE users_games
                ADD COLUMN IF NOT EXISTS "AchievementsSyncedAt" timestamp with time zone;
                """);

            migrationBuilder.AddColumn<bool>(
                name: "AchievementSyncUnavailable",
                table: "users_games",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "NeedsAchievementRefresh",
                table: "users_games",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "achievement_sync_coverage",
                table: "users",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "steam_library_public",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "HeaderImageUrl",
                table: "games",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SchemaSyncedAt",
                table: "games",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_games_UserId_AchievementSyncUnavailable",
                table: "users_games",
                columns: new[] { "UserId", "AchievementSyncUnavailable" });

            migrationBuilder.CreateIndex(
                name: "IX_users_games_UserId_NeedsAchievementRefresh",
                table: "users_games",
                columns: new[] { "UserId", "NeedsAchievementRefresh" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_users_games_UserId_AchievementSyncUnavailable",
                table: "users_games");

            migrationBuilder.DropIndex(
                name: "IX_users_games_UserId_NeedsAchievementRefresh",
                table: "users_games");

            migrationBuilder.DropColumn(
                name: "AchievementSyncUnavailable",
                table: "users_games");

            migrationBuilder.DropColumn(
                name: "NeedsAchievementRefresh",
                table: "users_games");

            migrationBuilder.Sql(
                """
                ALTER TABLE users_games
                DROP COLUMN IF EXISTS "AchievementsSyncedAt";
                """);

            migrationBuilder.DropColumn(
                name: "achievement_sync_coverage",
                table: "users");

            migrationBuilder.DropColumn(
                name: "steam_library_public",
                table: "users");

            migrationBuilder.DropColumn(
                name: "HeaderImageUrl",
                table: "games");

            migrationBuilder.DropColumn(
                name: "SchemaSyncedAt",
                table: "games");
        }
    }
}
