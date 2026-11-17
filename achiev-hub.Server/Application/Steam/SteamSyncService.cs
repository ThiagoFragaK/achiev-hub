using achiev_hub.Server.Application.Achievements.Interfaces;
using achiev_hub.Server.Application.Common;
using achiev_hub.Server.Application.Common.Interfaces;
using achiev_hub.Server.Application.Games;
using achiev_hub.Server.Application.Games.Interfaces;
using achiev_hub.Server.Application.Goals.Interfaces;
using achiev_hub.Server.Application.Steam.Interfaces;
using achiev_hub.Server.Application.Users.Interfaces;
using achiev_hub.Server.Domain.Entities;
using achiev_hub.Server.Infrastructure.Auth;
using Microsoft.Extensions.Caching.Memory;

namespace achiev_hub.Server.Application.Steam;

public class SteamSyncService : ISteamSyncService
{
    private const int AchievementSyncCooldownMinutes = 60;
    private const int ManualSyncCooldownMinutes = 30;
    private const int ManualSyncMaxPerDay = 10;
    private const int GlobalPercentTtlHours = 24;

    private readonly IUserRepository _users;
    private readonly IGameRepository _games;
    private readonly IUsersGameRepository _usersGames;
    private readonly IAchievementRepository _achievements;
    private readonly IUsersAchievementRepository _usersAchievements;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISteamApiClient _steamApiClient;
    private readonly IMemoryCache _cache;
    private readonly IGoalService _goals;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<SteamSyncService> _logger;

    public SteamSyncService(
        IUserRepository users,
        IGameRepository games,
        IUsersGameRepository usersGames,
        IAchievementRepository achievements,
        IUsersAchievementRepository usersAchievements,
        IUnitOfWork unitOfWork,
        ISteamApiClient steamApiClient,
        IMemoryCache cache,
        IGoalService goals,
        IHostEnvironment environment,
        ILogger<SteamSyncService> logger)
    {
        _users = users;
        _games = games;
        _usersGames = usersGames;
        _achievements = achievements;
        _usersAchievements = usersAchievements;
        _unitOfWork = unitOfWork;
        _steamApiClient = steamApiClient;
        _cache = cache;
        _goals = goals;
        _environment = environment;
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

        var user = await _users.GetTrackedByIdAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException($"User {userId} was not found.");

        var recentGames = await _steamApiClient.GetRecentlyPlayedGamesAsync(steamId, cancellationToken);
        var byAppId = new Dictionary<int, SyncGameInput>();

        if (scope == LibrarySyncScope.Full)
        {
            var ownedGames = await _steamApiClient.GetOwnedGamesAsync(steamId, cancellationToken: cancellationToken);
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
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _cache.Remove(RecentGamesCacheKeys.ForSteamId(steamId));
            _logger.LogInformation(
                "Synced library ({Scope}) for user {UserId}: 0 games",
                scope,
                userId);
            return;
        }

        var steamIds = byAppId.Keys.Select(id => id.ToString()).ToList();
        var existingGames = await _games.GetTrackedBySteamIdsAsync(steamIds, cancellationToken);

        var gamesBySteamId = existingGames
            .ToDictionary(g => g.GameSteamId, StringComparer.Ordinal);

        foreach (var input in byAppId.Values)
        {
            var steamAppId = input.AppId.ToString();
            var iconHash = SteamMediaUrls.ToStorageHash(input.ImageUrl);
            if (!gamesBySteamId.TryGetValue(steamAppId, out var game))
            {
                game = new Game
                {
                    GameSteamId = steamAppId,
                    Name = string.IsNullOrWhiteSpace(input.Name) ? steamAppId : input.Name.Trim(),
                    ImageUrl = iconHash,
                    HasCommunityVisibleStats = input.HasCommunityVisibleStats
                };
                _games.Add(game);
                gamesBySteamId[steamAppId] = game;
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(input.Name))
                {
                    game.Name = input.Name.Trim();
                }

                if (!string.IsNullOrWhiteSpace(iconHash))
                {
                    game.ImageUrl = iconHash;
                }

                if (input.HasCommunityVisibleStats.HasValue)
                {
                    game.HasCommunityVisibleStats = input.HasCommunityVisibleStats;
                }
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var persistedGameIds = gamesBySteamId.Values.Select(g => g.Id).ToList();
        var existingUsersGames = await _usersGames.GetTrackedByUserAndGameIdsAsync(
            userId,
            persistedGameIds,
            cancellationToken);

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
                _usersGames.Add(usersGame);
                usersGamesByGameId[game.Id] = usersGame;
            }

            usersGame.PlaytimeMinutes = input.PlaytimeMinutes;
            if (input.LastPlayedUnix.HasValue)
            {
                usersGame.LastPlayedUnix = input.LastPlayedUnix;
            }
        }

        user.Playtime2WeeksMinutes = recentGames.Sum(g => g.Playtime2WeeksMinutes);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

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
        bool isManualRefresh = false,
        CancellationToken cancellationToken = default)
    {
        if (appId <= 0)
        {
            return;
        }

        var steamAppId = appId.ToString();
        var game = await _games.GetTrackedBySteamIdAsync(steamAppId, cancellationToken);

        if (game is null || game.HasCommunityVisibleStats != true)
        {
            _logger.LogDebug(
                "Skipping achievement sync for app {AppId}: game missing or no community stats",
                appId);
            return;
        }

        var usersGame = await _usersGames.GetTrackedByUserAndGameAsync(userId, game.Id, cancellationToken);

        if (usersGame is null)
        {
            _logger.LogDebug(
                "Skipping achievement sync for app {AppId}: no UsersGame for user {UserId}",
                appId,
                userId);
            return;
        }

        if (isManualRefresh)
        {
            EnforceManualSyncRateLimits(userId);
        }
        else if (usersGame.AchievementsSyncedAt is DateTimeOffset lastSynced)
        {
            var elapsed = DateTimeOffset.UtcNow - lastSynced;
            var cooldown = TimeSpan.FromMinutes(AchievementSyncCooldownMinutes);
            if (elapsed < cooldown)
            {
                var remaining = (int)Math.Ceiling((cooldown - elapsed).TotalSeconds);
                throw new SteamSyncCooldownException(Math.Max(1, remaining));
            }
        }

        if (isManualRefresh)
        {
            await RefreshPlaytimeForGameAsync(usersGame, steamId, appId, cancellationToken);
        }

        var schema = await _steamApiClient.GetGameSchemaAsync(appId, cancellationToken);
        var schemaAchievements = (schema?.Achievements ?? [])
            .Where(a => !string.IsNullOrWhiteSpace(a.Name))
            .GroupBy(a => a.Name!, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();

        var existingCatalog = await _achievements.GetTrackedByGameIdAsync(game.Id, cancellationToken);

        var catalogByApiName = existingCatalog
            .ToDictionary(a => a.ApiName, StringComparer.OrdinalIgnoreCase);

        foreach (var schemaRow in schemaAchievements)
        {
            var apiName = schemaRow.Name!.Trim();
            if (apiName.Length == 0)
            {
                continue;
            }

            if (!catalogByApiName.TryGetValue(apiName, out var achievement))
            {
                achievement = new Achievement
                {
                    GameId = game.Id,
                    ApiName = apiName
                };
                _achievements.Add(achievement);
                catalogByApiName[apiName] = achievement;
            }

            achievement.Name = string.IsNullOrWhiteSpace(schemaRow.DisplayName)
                ? apiName
                : schemaRow.DisplayName.Trim();
            achievement.Description = schemaRow.Hidden == 1 && string.IsNullOrWhiteSpace(schemaRow.Description)
                ? null
                : schemaRow.Description;
            achievement.ImageUrlLock = SteamMediaUrls.ToStorageHash(schemaRow.IconGray);
            achievement.ImageUrlUnlock = SteamMediaUrls.ToStorageHash(schemaRow.Icon);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await SyncGlobalPercentagesAsync(game, appId, cancellationToken);

        var playerResult = await _steamApiClient.GetPlayerAchievementsAsync(steamId, appId, cancellationToken);
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

        var refreshedCatalog = await _achievements.GetTrackedByGameIdAsync(game.Id, cancellationToken);

        var achievementIdsByApiName = refreshedCatalog
            .ToDictionary(a => a.ApiName, a => a.Id, StringComparer.OrdinalIgnoreCase);

        var existingUnlocks = await _usersAchievements.GetTrackedByUserAndGameAsync(
            userId,
            game.Id,
            cancellationToken);

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
                _usersAchievements.Add(usersAchievement);
            }

            usersAchievement.UnlockDate = playerRow.UnlockTimeUnix > 0
                ? DateTimeOffset.FromUnixTimeSeconds(playerRow.UnlockTimeUnix).UtcDateTime
                : null;
        }

        foreach (var stale in existingUnlocks.Where(ua => !unlockedAchievementIds.Contains(ua.AchievementId)))
        {
            _usersAchievements.Remove(stale);
        }

        var totalAchievements = refreshedCatalog.Count;
        var unlockedCount = unlockedAchievementIds.Count;
        usersGame.AchievementsPercentage = totalAchievements == 0
            ? 0
            : (decimal)Math.Round(unlockedCount / (double)totalAchievements * 100, 2);
        usersGame.AchievementsSyncedAt = DateTimeOffset.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await RecalculateUserAvgPercentageAsync(userId, cancellationToken);
        await _goals.RefreshForGameAsync(userId, game.Id, cancellationToken);

        if (isManualRefresh)
        {
            RecordManualSync(userId);
        }

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
                await SyncGameAchievementsAsync(userId, steamId, appId, cancellationToken: cancellationToken);
            }
            catch (SteamSyncCooldownException)
            {
                _logger.LogDebug(
                    "Skipping achievement sync for user {UserId} app {AppId}: within cooldown",
                    userId,
                    appId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Achievement sync failed for user {UserId} app {AppId}", userId, appId);
            }
        }
    }

    private async Task SyncGlobalPercentagesAsync(Game game, int appId, CancellationToken cancellationToken)
    {
        var ttl = TimeSpan.FromHours(GlobalPercentTtlHours);
        if (game.GlobalPercentSyncedAt.HasValue &&
            game.GlobalPercentSyncedAt.Value > DateTimeOffset.UtcNow - ttl)
        {
            _logger.LogDebug("Global percent for app {AppId} is within TTL", appId);
            return;
        }

        try
        {
            var catalog = await _achievements.GetTrackedByGameIdAsync(game.Id, cancellationToken);

            if (catalog.Count == 0)
            {
                game.GlobalPercentSyncedAt = DateTimeOffset.UtcNow;
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return;
            }

            var percentages = await _steamApiClient.GetGlobalAchievementPercentagesAsync(appId, cancellationToken);
            if (percentages is null)
            {
                _logger.LogWarning("Global achievement percentages unavailable for app {AppId}", appId);
                return;
            }

            var byName = percentages
                .Where(p => !string.IsNullOrWhiteSpace(p.Name))
                .GroupBy(p => p.Name!, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().Percent, StringComparer.OrdinalIgnoreCase);

            var updated = 0;
            foreach (var achievement in catalog)
            {
                if (string.IsNullOrWhiteSpace(achievement.ApiName) ||
                    !byName.TryGetValue(achievement.ApiName, out var percent))
                {
                    continue;
                }

                achievement.GlobalPercentage = RoundGlobalPercentage(percent);
                updated++;
            }

            game.GlobalPercentSyncedAt = DateTimeOffset.UtcNow;
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Updated global percentages for app {AppId}: {Updated}/{Catalog}",
                appId,
                updated,
                catalog.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Global percentage sync failed for app {AppId}; continuing achievement sync", appId);
        }
    }

    private static decimal RoundGlobalPercentage(decimal percent)
    {
        var rounded = Math.Round(percent, 2, MidpointRounding.AwayFromZero);
        if (rounded < 0m)
        {
            return 0m;
        }

        if (rounded > 999.99m)
        {
            return 999.99m;
        }

        return rounded;
    }

    private async Task RecalculateUserAvgPercentageAsync(int userId, CancellationToken cancellationToken)
    {
        var (average, gamesInAverage) = await _usersGames.GetAveragePercentageStatsAsync(userId, cancellationToken);

        var user = await _users.GetTrackedByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return;
        }

        user.AvgPercentage = gamesInAverage == 0 ? 0 : average;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<List<int>> GetAllOwnedWithStatsAppIdsAsync(int userId, CancellationToken cancellationToken)
    {
        var steamIds = await _usersGames.GetOwnedWithStatsSteamIdsAsync(userId, cancellationToken);

        var appIds = new List<int>();
        foreach (var id in steamIds)
        {
            if (int.TryParse(id, out var appId) && appId > 0)
            {
                appIds.Add(appId);
            }
        }

        return appIds;
    }

    private async Task<List<int>> GetRecentTwoWeeksAppIdsAsync(string steamId, CancellationToken cancellationToken)
    {
        var recent = await _steamApiClient.GetRecentlyPlayedGamesAsync(steamId, cancellationToken);
        return recent
            .Where(g => g.AppId > 0 && g.Playtime2WeeksMinutes > 0)
            .Select(g => g.AppId)
            .Distinct()
            .ToList();
    }

    private void EnforceManualSyncRateLimits(int userId)
    {
        if (_environment.IsDevelopment())
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var state = _cache.Get<ManualSyncState>(ManualSyncCacheKey(userId));

        if (state is null || state.Day != today)
        {
            return;
        }

        if (state.Count >= ManualSyncMaxPerDay)
        {
            var nextMidnight = new DateTimeOffset(today.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            var retryAfter = (int)Math.Ceiling((nextMidnight - now).TotalSeconds);
            throw SteamSyncCooldownException.DailyLimitReached(Math.Max(1, retryAfter));
        }

        var cooldown = TimeSpan.FromMinutes(ManualSyncCooldownMinutes);
        var elapsed = now - state.LastSyncAt;
        if (elapsed < cooldown)
        {
            var remaining = (int)Math.Ceiling((cooldown - elapsed).TotalSeconds);
            throw SteamSyncCooldownException.SpacingNotElapsed(Math.Max(1, remaining));
        }
    }

    private void RecordManualSync(int userId)
    {
        if (_environment.IsDevelopment())
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var key = ManualSyncCacheKey(userId);
        var existing = _cache.Get<ManualSyncState>(key);
        var count = existing is not null && existing.Day == today
            ? existing.Count + 1
            : 1;

        var state = new ManualSyncState(today, count, now);
        var untilMidnight = new DateTimeOffset(today.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero) - now;
        _cache.Set(key, state, untilMidnight > TimeSpan.Zero ? untilMidnight : TimeSpan.FromDays(1));
    }

    private async Task RefreshPlaytimeForGameAsync(
        UsersGame usersGame,
        string steamId,
        int appId,
        CancellationToken cancellationToken)
    {
        var ownedGames = await _steamApiClient.GetOwnedGamesAsync(
            steamId,
            appIdsFilter: [appId],
            cancellationToken: cancellationToken);

        var owned = ownedGames.FirstOrDefault(g => g.AppId == appId);
        if (owned is null)
        {
            _logger.LogDebug(
                "Playtime refresh skipped for app {AppId}: not returned by GetOwnedGames",
                appId);
            return;
        }

        usersGame.PlaytimeMinutes = owned.PlaytimeForeverMinutes;
        if (owned.LastPlayedUnix.HasValue)
        {
            usersGame.LastPlayedUnix = owned.LastPlayedUnix;
        }
    }

    private static string ManualSyncCacheKey(int userId) => $"manual-game-sync:{userId}";

    private sealed record ManualSyncState(DateOnly Day, int Count, DateTimeOffset LastSyncAt);

    private sealed record SyncGameInput(
        int AppId,
        string? Name,
        string? ImageUrl,
        int PlaytimeMinutes,
        long? LastPlayedUnix,
        bool? HasCommunityVisibleStats);
}
