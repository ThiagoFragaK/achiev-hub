using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace achiev_hub.Server.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OptimizeDbStorageAndIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) Remove rows that cannot satisfy NOT NULL + unique business keys.
            migrationBuilder.Sql(
                """
                DELETE FROM users_achievements ua
                USING achievements a
                WHERE ua."AchievementId" = a."Id"
                  AND (a."ApiName" IS NULL OR btrim(a."ApiName") = '');

                DELETE FROM goal_achievements ga
                USING achievements a
                WHERE ga."AchievementId" = a."Id"
                  AND (a."ApiName" IS NULL OR btrim(a."ApiName") = '');

                DELETE FROM achievements
                WHERE "ApiName" IS NULL OR btrim("ApiName") = '';

                DELETE FROM users_games ug
                USING games g
                WHERE ug."GameId" = g."Id"
                  AND (g."GameSteamId" IS NULL OR btrim(g."GameSteamId") = '');

                DELETE FROM goals go
                USING games g
                WHERE go."GameId" = g."Id"
                  AND (g."GameSteamId" IS NULL OR btrim(g."GameSteamId") = '');

                DELETE FROM users_achievements ua
                USING games g
                WHERE ua."GameId" = g."Id"
                  AND (g."GameSteamId" IS NULL OR btrim(g."GameSteamId") = '');

                DELETE FROM achievements a
                USING games g
                WHERE a."GameId" = g."Id"
                  AND (g."GameSteamId" IS NULL OR btrim(g."GameSteamId") = '');

                DELETE FROM games
                WHERE "GameSteamId" IS NULL OR btrim("GameSteamId") = '';
                """);

            // 2) Trim CDN URLs to 40-char hashes while columns are still varchar(500).
            // Batched to avoid Npgsql default 30s command timeout on large catalogs.
            migrationBuilder.Sql(
                """
                SET statement_timeout = 0;
                DO $backfill$
                DECLARE
                  batch INT := 20000;
                  max_id INT;
                  cur INT := 0;
                BEGIN
                  SELECT COALESCE(MAX("Id"), 0) INTO max_id FROM achievements;
                  WHILE cur <= max_id LOOP
                    UPDATE achievements
                    SET "ImageUrlLock" = CASE
                          WHEN "ImageUrlLock" ~* '^https?://' THEN
                            substring("ImageUrlLock" from '([0-9a-f]{40})(?:\.jpe?g)?(?:\?.*)?$')
                          WHEN "ImageUrlLock" ~* '\.jpe?g$' THEN
                            regexp_replace("ImageUrlLock", '\.jpe?g$', '', 'i')
                          ELSE "ImageUrlLock"
                        END,
                        "ImageUrlUnlock" = CASE
                          WHEN "ImageUrlUnlock" ~* '^https?://' THEN
                            substring("ImageUrlUnlock" from '([0-9a-f]{40})(?:\.jpe?g)?(?:\?.*)?$')
                          WHEN "ImageUrlUnlock" ~* '\.jpe?g$' THEN
                            regexp_replace("ImageUrlUnlock", '\.jpe?g$', '', 'i')
                          ELSE "ImageUrlUnlock"
                        END
                    WHERE "Id" > cur AND "Id" <= cur + batch
                      AND (
                        ("ImageUrlLock" IS NOT NULL AND length("ImageUrlLock") > 40)
                        OR ("ImageUrlUnlock" IS NOT NULL AND length("ImageUrlUnlock") > 40)
                      );
                    cur := cur + batch;
                  END LOOP;
                END
                $backfill$;

                UPDATE games
                SET "ImageUrl" = CASE
                      WHEN "ImageUrl" ~* '^https?://' THEN
                        substring("ImageUrl" from '([0-9a-f]{40})(?:\.jpe?g)?(?:\?.*)?$')
                      WHEN "ImageUrl" ~* '\.jpe?g$' THEN
                        regexp_replace("ImageUrl", '\.jpe?g$', '', 'i')
                      ELSE "ImageUrl"
                    END
                WHERE "ImageUrl" IS NOT NULL AND length("ImageUrl") > 40;

                UPDATE games SET "HeaderImageUrl" = NULL WHERE "HeaderImageUrl" IS NOT NULL;
                """);

            migrationBuilder.DropIndex(
                name: "IX_games_GameSteamId",
                table: "games");

            migrationBuilder.DropIndex(
                name: "IX_achievements_GameId_ApiName",
                table: "achievements");

            migrationBuilder.AlterColumn<string>(
                name: "ImageUrl",
                table: "games",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "HeaderImageUrl",
                table: "games",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "GameSteamId",
                table: "games",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ImageUrlUnlock",
                table: "achievements",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ImageUrlLock",
                table: "achievements",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ApiName",
                table: "achievements",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_achievements_UserId_GameId",
                table: "users_achievements",
                columns: new[] { "UserId", "GameId" });

            migrationBuilder.CreateIndex(
                name: "IX_users_achievements_UserId_unlock_date",
                table: "users_achievements",
                columns: new[] { "UserId", "unlock_date" },
                filter: "unlock_date IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_games_GameSteamId",
                table: "games",
                column: "GameSteamId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_achievements_GameId_ApiName",
                table: "achievements",
                columns: new[] { "GameId", "ApiName" },
                unique: true);

            migrationBuilder.Sql("ANALYZE achievements; ANALYZE users_achievements; ANALYZE games; ANALYZE users_games;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_users_achievements_UserId_GameId",
                table: "users_achievements");

            migrationBuilder.DropIndex(
                name: "IX_users_achievements_UserId_unlock_date",
                table: "users_achievements");

            migrationBuilder.DropIndex(
                name: "IX_games_GameSteamId",
                table: "games");

            migrationBuilder.DropIndex(
                name: "IX_achievements_GameId_ApiName",
                table: "achievements");

            migrationBuilder.AlterColumn<string>(
                name: "ImageUrl",
                table: "games",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "HeaderImageUrl",
                table: "games",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "GameSteamId",
                table: "games",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "ImageUrlUnlock",
                table: "achievements",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ImageUrlLock",
                table: "achievements",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ApiName",
                table: "achievements",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.CreateIndex(
                name: "IX_games_GameSteamId",
                table: "games",
                column: "GameSteamId",
                unique: true,
                filter: "\"GameSteamId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_achievements_GameId_ApiName",
                table: "achievements",
                columns: new[] { "GameId", "ApiName" },
                unique: true,
                filter: "\"ApiName\" IS NOT NULL AND \"ApiName\" <> ''");
        }
    }
}
