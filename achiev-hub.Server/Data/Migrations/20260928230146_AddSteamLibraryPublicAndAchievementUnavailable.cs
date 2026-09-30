using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace achiev_hub.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSteamLibraryPublicAndAchievementUnavailable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AchievementSyncUnavailable",
                table: "users_games",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "steam_library_public",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_games_UserId_AchievementSyncUnavailable",
                table: "users_games",
                columns: new[] { "UserId", "AchievementSyncUnavailable" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_users_games_UserId_AchievementSyncUnavailable",
                table: "users_games");

            migrationBuilder.DropColumn(
                name: "AchievementSyncUnavailable",
                table: "users_games");

            migrationBuilder.DropColumn(
                name: "steam_library_public",
                table: "users");
        }
    }
}
