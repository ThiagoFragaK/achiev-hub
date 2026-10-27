using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace achiev_hub.Server.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnforceNonEmptyBusinessKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Safety net if any blank keys slipped in after NOT NULL (no CHECK yet).
            migrationBuilder.Sql(
                """
                DELETE FROM users_achievements ua
                USING achievements a
                WHERE ua."AchievementId" = a."Id"
                  AND btrim(a."ApiName") = '';

                DELETE FROM goal_achievements ga
                USING achievements a
                WHERE ga."AchievementId" = a."Id"
                  AND btrim(a."ApiName") = '';

                DELETE FROM achievements
                WHERE btrim("ApiName") = '';

                DELETE FROM users_games ug
                USING games g
                WHERE ug."GameId" = g."Id"
                  AND btrim(g."GameSteamId") = '';

                DELETE FROM goals go
                USING games g
                WHERE go."GameId" = g."Id"
                  AND btrim(g."GameSteamId") = '';

                DELETE FROM users_achievements ua
                USING games g
                WHERE ua."GameId" = g."Id"
                  AND btrim(g."GameSteamId") = '';

                DELETE FROM achievements a
                USING games g
                WHERE a."GameId" = g."Id"
                  AND btrim(g."GameSteamId") = '';

                DELETE FROM games
                WHERE btrim("GameSteamId") = '';
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_achievements_ApiName_not_empty",
                table: "achievements",
                sql: "length(btrim(\"ApiName\")) > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_games_GameSteamId_not_empty",
                table: "games",
                sql: "length(btrim(\"GameSteamId\")) > 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_achievements_ApiName_not_empty",
                table: "achievements");

            migrationBuilder.DropCheckConstraint(
                name: "CK_games_GameSteamId_not_empty",
                table: "games");
        }
    }
}
