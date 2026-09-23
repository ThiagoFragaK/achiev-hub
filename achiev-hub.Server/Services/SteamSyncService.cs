using achiev_hub.Server.Data;
using achiev_hub.Server.Entities;
using achiev_hub.Server.Repositories.Interfaces;
using achiev_hub.Server.Services.Interfaces;
using achiev_hub.Server.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace achiev_hub.Server.Services;

public class SteamSyncService : ISteamSyncService
{
    private readonly ApplicationDbContext _db;
    private readonly ISteamRepository _steamRepository;
    private readonly IMemoryCache _cache;
    private readonly ILogger<SteamSyncService> _logger;

    public SteamSyncService(
        ApplicationDbContext db,
        ISteamRepository steamRepository,
        IMemoryCache cache,
        ILogger<SteamSyncService> logger)
    {
        _db = db;
        _steamRepository = steamRepository;
        _cache = cache;
        _logger = logger;
    }

    public async Task SyncLibraryAsync(
        int userId,
        string steamId,
        LibrarySyncScope scope = LibrarySyncScope.Full,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(steamId))
        {
            throw new ArgumentException("steamId is required.", nameof(steamId));
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new InvalidOperationException($"User {userId} was not found.");

        var recentGames = await _steamRepository.GetRecentlyPlayedGamesAsync(steamId, cancellationToken);
        var byAppId = new Dictionary<int, SyncGameInput>();

        if (scope == LibrarySyncScope.Full)
        {
            var ownedGames = await _steamRepository.GetOwnedGamesAsync(steamId, cancellationToken);
            foreach (var owned in ownedGames)
            {
                if (owned.AppId <= 0)
                {
                    continue;
                }

                byAppId[owned.AppId] = new SyncGameInput(
                    owned.AppId,
                    owned.Name,
                    owned.ImgIconUrl,
                    owned.PlaytimeForeverMinutes,
                    owned.LastPlayedUnix,
                    owned.HasCommunityVisibleStats);
            }
        }

        foreach (var recent in recentGames)
        {
            if (recent.AppId <= 0)
            {
                continue;
            }

            if (byAppId.TryGetValue(recent.AppId, out var existing))
            {
                byAppId[recent.AppId] = existing with
                {
                    Name = string.IsNullOrWhiteSpace(existing.Name) ? recent.Name : existing.Name,
                    ImageUrl = string.IsNullOrWhiteSpace(existing.ImageUrl) ? recent.ImgIconUrl : existing.ImageUrl,
                    PlaytimeMinutes = existing.PlaytimeMinutes > 0
                        ? existing.PlaytimeMinutes
                        : recent.PlaytimeForeverMinutes
                };
            }
            else
            {
                byAppId[recent.AppId] = new SyncGameInput(
                    recent.AppId,
                    recent.Name,
                    recent.ImgIconUrl,
                    recent.PlaytimeForeverMinutes,
                    null,
                    null);
            }
        }

        if (byAppId.Count == 0)
        {
            user.Playtime2WeeksMinutes = recentGames.Sum(g => g.Playtime2WeeksMinutes);
            await _db.SaveChangesAsync(cancellationToken);
            _cache.Remove(RecentGamesCacheKeys.ForSteamId(steamId));
            _logger.LogInformation(
                "Synced library ({Scope}) for user {UserId}: 0 games",
                scope,
                userId);
            return;
        }

        var steamIds = byAppId.Keys.Select(id => id.ToString()).ToList();
        var existingGames = await _db.Games
            .Where(g => g.GameSteamId != null && steamIds.Contains(g.GameSteamId))
            .ToListAsync(cancellationToken);

        var gamesBySteamId = existingGames
            .Where(g => g.GameSteamId is not null)
            .ToDictionary(g => g.GameSteamId!, StringComparer.Ordinal);

        foreach (var input in byAppId.Values)
        {
            var steamAppId = input.AppId.ToString();
            if (!gamesBySteamId.TryGetValue(steamAppId, out var game))
            {
                game = new Game
                {
                    GameSteamId = steamAppId,
                    Name = string.IsNullOrWhiteSpace(input.Name) ? steamAppId : input.Name.Trim(),
                    ImageUrl = input.ImageUrl,
                    HasCommunityVisibleStats = input.HasCommunityVisibleStats
                };
                _db.Games.Add(game);
                gamesBySteamId[steamAppId] = game;
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(input.Name))
                {
                    game.Name = input.Name.Trim();
                }

                if (!string.IsNullOrWhiteSpace(input.ImageUrl))
                {
                    game.ImageUrl = input.ImageUrl;
                }

                if (input.HasCommunityVisibleStats.HasValue)
                {
                    game.HasCommunityVisibleStats = input.HasCommunityVisibleStats;
                }
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        var persistedGameIds = gamesBySteamId.Values.Select(g => g.Id).ToList();
        var existingUsersGames = await _db.UsersGames
            .Where(ug => ug.UserId == userId && persistedGameIds.Contains(ug.GameId))
            .ToListAsync(cancellationToken);

        var usersGamesByGameId = existingUsersGames.ToDictionary(ug => ug.GameId);

        foreach (var input in byAppId.Values)
        {
            var steamAppId = input.AppId.ToString();
            var game = gamesBySteamId[steamAppId];

            if (!usersGamesByGameId.TryGetValue(game.Id, out var usersGame))
            {
                usersGame = new UsersGame
                {
                    UserId = userId,
                    GameId = game.Id,
                    AchievementsPercentage = 0
                };
                _db.UsersGames.Add(usersGame);
                usersGamesByGameId[game.Id] = usersGame;
            }

            usersGame.PlaytimeMinutes = input.PlaytimeMinutes;
            if (input.LastPlayedUnix.HasValue)
            {
                usersGame.LastPlayedUnix = input.LastPlayedUnix;
            }
        }

        user.Playtime2WeeksMinutes = recentGames.Sum(g => g.Playtime2WeeksMinutes);

        await _db.SaveChangesAsync(cancellationToken);

        _cache.Remove(RecentGamesCacheKeys.ForSteamId(steamId));

        _logger.LogInformation(
            "Synced library ({Scope}) for user {UserId}: {GameCount} games, Playtime2WeeksMinutes={Playtime2Weeks}",
            scope,
            userId,
            byAppId.Count,
            user.Playtime2WeeksMinutes);
    }

    public async Task SyncGameAchievementsAsync(
        int userId,
        string steamId,
        int appId,
        CancellationToken cancellationToken = default)
    {
        if (appId <= 0)
        {
            return;
        }

        var steamAppId = appId.ToString();
        var game = await _db.Games
            .FirstOrDefaultAsync(g => g.GameSteamId == steamAppId, cancellationToken);

        if (game is null || game.HasCommunityVisibleStats != true)
        {
            _logger.LogDebug(
                "Skipping achievement sync for app {AppId}: game missing or no community stats",
                appId);
            return;
        }

        var usersGame = await _db.UsersGames
            .FirstOrDefaultAsync(ug => ug.UserId == userId && ug.GameId == game.Id, cancellationToken);

        if (usersGame is null)
        {
            _logger.LogDebug(
                "Skipping achievement sync for app {AppId}: no UsersGame for user {UserId}",
                appId,
                userId);
            return;
        }

        var schema = await _steamRepository.GetGameSchemaAsync(appId, cancellationToken);
        var schemaAchievements = (schema?.Achievements ?? [])
            .Where(a => !string.IsNullOrWhiteSpace(a.Name))
            .GroupBy(a => a.Name!, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();

        var existingCatalog = await _db.Achievements
            .Where(a => a.GameId == game.Id)
            .ToListAsync(cancellationToken);

        var catalogByApiName = existingCatalog
            .Where(a => !string.IsNullOrWhiteSpace(a.ApiName))
            .ToDictionary(a => a.ApiName!, StringComparer.OrdinalIgnoreCase);

        foreach (var schemaRow in schemaAchievements)
        {
            var apiName = schemaRow.Name!;
            if (!catalogByApiName.TryGetValue(apiName, out var achievement))
            {
                achievement = new Achievement
                {
                    GameId = game.Id,
                    ApiName = apiName
                };
                _db.Achievements.Add(achievement);
                catalogByApiName[apiName] = achievement;
            }

            achievement.Name = string.IsNullOrWhiteSpace(schemaRow.DisplayName)
                ? apiName
                : schemaRow.DisplayName.Trim();
            achievement.Description = schemaRow.Hidden == 1 && string.IsNullOrWhiteSpace(schemaRow.Description)
                ? null
                : schemaRow.Description;
            achievement.ImageUrlLock = schemaRow.IconGray;
            achievement.ImageUrlUnlock = schemaRow.Icon;
        }

        await _db.SaveChangesAsync(cancellationToken);

        var playerResult = await _steamRepository.GetPlayerAchievementsAsync(steamId, appId, cancellationToken);
        if (playerResult is null || !playerResult.Success)
        {
            _logger.LogWarning(
                "Player achievements unavailable for user {UserId} app {AppId}: {Error}",
                userId,
                appId,
                playerResult?.Error);
            return;
        }

        var unlockedApiNames = playerResult.Achievements
            .Where(a => a.Achieved == 1 && !string.IsNullOrWhiteSpace(a.ApiName))
            .ToDictionary(a => a.ApiName!, a => a, StringComparer.OrdinalIgnoreCase);

        var refreshedCatalog = await _db.Achievements
            .Where(a => a.GameId == game.Id)
            .ToListAsync(cancellationToken);

        var achievementIdsByApiName = refreshedCatalog
            .Where(a => !string.IsNullOrWhiteSpace(a.ApiName))
            .ToDictionary(a => a.ApiName!, a => a.Id, StringComparer.OrdinalIgnoreCase);

        var existingUnlocks = await _db.UsersAchievements
            .Where(ua => ua.UserId == userId && ua.GameId == game.Id)
            .ToListAsync(cancellationToken);

        var unlocksByAchievementId = existingUnlocks.ToDictionary(ua => ua.AchievementId);

        var unlockedAchievementIds = new HashSet<int>();

        foreach (var (apiName, playerRow) in unlockedApiNames)
        {
            if (!achievementIdsByApiName.TryGetValue(apiName, out var achievementId))
            {
                continue;
            }

            unlockedAchievementIds.Add(achievementId);

            if (!unlocksByAchievementId.TryGetValue(achievementId, out var usersAchievement))
            {
                usersAchievement = new UsersAchievement
                {
                    UserId = userId,
                    GameId = game.Id,
                    AchievementId = achievementId
                };
                _db.UsersAchievements.Add(usersAchievement);
            }

            usersAchievement.UnlockDate = playerRow.UnlockTimeUnix > 0
                ? DateTimeOffset.FromUnixTimeSeconds(playerRow.UnlockTimeUnix).UtcDateTime
                : null;
        }

        foreach (var stale in existingUnlocks.Where(ua => !unlockedAchievementIds.Contains(ua.AchievementId)))
        {
            _db.UsersAchievements.Remove(stale);
        }

        var totalAchievements = refreshedCatalog.Count;
        var unlockedCount = unlockedAchievementIds.Count;
        usersGame.AchievementsPercentage = totalAchievements == 0
            ? 0
            : (decimal)Math.Round(unlockedCount / (double)totalAchievements * 100, 2);

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Synced achievements for user {UserId} app {AppId}: {Unlocked}/{Total}",
            userId,
            appId,
            unlockedCount,
            totalAchievements);
    }

    public async Task SyncAchievementsForUserAsync(
        int userId,
        string steamId,
        AchievementSyncScope scope,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<int> appIds = scope switch
        {
            AchievementSyncScope.AllOwnedWithStats => await GetAllOwnedWithStatsAppIdsAsync(userId, cancellationToken),
            AchievementSyncScope.RecentTwoWeeks => await GetRecentTwoWeeksAppIdsAsync(steamId, cancellationToken),
            _ => Array.Empty<int>()
        };

        foreach (var appId in appIds)
        {
            try
            {
                await SyncGameAchievementsAsync(userId, steamId, appId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Achievement sync failed for user {UserId} app {AppId}", userId, appId);
            }
        }
    }

    private async Task<List<int>> GetAllOwnedWithStatsAppIdsAsync(int userId, CancellationToken cancellationToken)
    {
        var steamIds = await _db.UsersGames
            .AsNoTracking()
            .Where(ug => ug.UserId == userId && ug.Game.HasCommunityVisibleStats == true)
            .Select(ug => ug.Game.GameSteamId)
            .ToListAsync(cancellationToken);

        var appIds = new List<int>();
        foreach (var id in steamIds)
        {
            if (id is not null && int.TryParse(id, out var appId) && appId > 0)
            {
                appIds.Add(appId);
            }
        }

        return appIds;
    }

    private async Task<List<int>> GetRecentTwoWeeksAppIdsAsync(string steamId, CancellationToken cancellationToken)
    {
        var recent = await _steamRepository.GetRecentlyPlayedGamesAsync(steamId, cancellationToken);
        return recent
            .Where(g => g.AppId > 0 && g.Playtime2WeeksMinutes > 0)
            .Select(g => g.AppId)
            .Distinct()
            .ToList();
    }

    private sealed record SyncGameInput(
        int AppId,
        string? Name,
        string? ImageUrl,
        int PlaytimeMinutes,
        long? LastPlayedUnix,
        bool? HasCommunityVisibleStats);
}
