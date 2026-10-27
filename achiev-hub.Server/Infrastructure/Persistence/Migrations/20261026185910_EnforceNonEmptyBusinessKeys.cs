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
            // Remove any blank keys that would fail the CHECKs (NOT NULL already applied).
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

            // Idempotent: constraint may already exist if an older duplicate migration
            // (e.g. 20261026180000_EnforceNonEmptyBusinessKeys) was applied before the merge cleanup.
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint
                        WHERE conname = 'CK_games_GameSteamId_not_empty'
                    ) THEN
                        ALTER TABLE games
                        ADD CONSTRAINT "CK_games_GameSteamId_not_empty"
                        CHECK (length(btrim("GameSteamId")) > 0);
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint
                        WHERE conname = 'CK_achievements_ApiName_not_empty'
                    ) THEN
                        ALTER TABLE achievements
                        ADD CONSTRAINT "CK_achievements_ApiName_not_empty"
                        CHECK (length(btrim("ApiName")) > 0);
                    END IF;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE games DROP CONSTRAINT IF EXISTS "CK_games_GameSteamId_not_empty";
                ALTER TABLE achievements DROP CONSTRAINT IF EXISTS "CK_achievements_ApiName_not_empty";
                """);
        }
    }
}
