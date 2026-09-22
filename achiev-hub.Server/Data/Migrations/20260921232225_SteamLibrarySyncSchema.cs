using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace achiev_hub.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class SteamLibrarySyncSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_achievements_GameId",
                table: "achievements");

            migrationBuilder.DropColumn(
                name: "PlayTime",
                table: "games");

            migrationBuilder.AddColumn<long>(
                name: "LastPlayedUnix",
                table: "users_games",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PlaytimeMinutes",
                table: "users_games",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Playtime2WeeksMinutes",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Developers",
                table: "games",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Publishers",
                table: "games",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApiName",
                table: "achievements",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_achievements_GameId_ApiName",
                table: "achievements",
                columns: new[] { "GameId", "ApiName" },
                unique: true,
                filter: "\"ApiName\" IS NOT NULL AND \"ApiName\" <> ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_achievements_GameId_ApiName",
                table: "achievements");

            migrationBuilder.DropColumn(
                name: "LastPlayedUnix",
                table: "users_games");

            migrationBuilder.DropColumn(
                name: "PlaytimeMinutes",
                table: "users_games");

            migrationBuilder.DropColumn(
                name: "Playtime2WeeksMinutes",
                table: "users");

            migrationBuilder.DropColumn(
                name: "Developers",
                table: "games");

            migrationBuilder.DropColumn(
                name: "Publishers",
                table: "games");

            migrationBuilder.DropColumn(
                name: "ApiName",
                table: "achievements");

            migrationBuilder.AddColumn<int>(
                name: "PlayTime",
                table: "games",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_achievements_GameId",
                table: "achievements",
                column: "GameId");
        }
    }
}
