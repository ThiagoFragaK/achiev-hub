using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace achiev_hub.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSyncJobsAndWatermarks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AchievementsSyncedAt",
                table: "users_games",
                type: "timestamp with time zone",
                nullable: true);

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

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SchemaSyncedAt",
                table: "games",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "sync_jobs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: true),
                    SteamId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    AppId = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Cursor = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    ProgressDone = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    ProgressTotal = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    Attempts = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    LastError = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AvailableAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sync_jobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sync_jobs_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_users_games_UserId_NeedsAchievementRefresh",
                table: "users_games",
                columns: new[] { "UserId", "NeedsAchievementRefresh" });

            migrationBuilder.CreateIndex(
                name: "IX_sync_jobs_Status_AvailableAt",
                table: "sync_jobs",
                columns: new[] { "Status", "AvailableAt" });

            migrationBuilder.CreateIndex(
                name: "IX_sync_jobs_UserId_Type_Status",
                table: "sync_jobs",
                columns: new[] { "UserId", "Type", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sync_jobs");

            migrationBuilder.DropIndex(
                name: "IX_users_games_UserId_NeedsAchievementRefresh",
                table: "users_games");

            migrationBuilder.DropColumn(
                name: "AchievementsSyncedAt",
                table: "users_games");

            migrationBuilder.DropColumn(
                name: "NeedsAchievementRefresh",
                table: "users_games");

            migrationBuilder.DropColumn(
                name: "achievement_sync_coverage",
                table: "users");

            migrationBuilder.DropColumn(
                name: "SchemaSyncedAt",
                table: "games");
        }
    }
}
