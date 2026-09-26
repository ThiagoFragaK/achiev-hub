using achiev_hub.Server.Services;

namespace achiev_hub.Server.Services.Interfaces;

public interface ISteamSyncService
{
    Task SyncLibraryAsync(
        int userId,
        string steamId,
        LibrarySyncScope scope = LibrarySyncScope.Full,
        int maxStoreEnrich = 5,
        CancellationToken cancellationToken = default);

    Task SyncGameAchievementsAsync(int userId, string steamId, int appId, CancellationToken cancellationToken = default);

    Task SyncGameCompletionPercentageAsync(
        int userId,
        string steamId,
        int appId,
        CancellationToken cancellationToken = default);

    Task SyncAchievementsForUserAsync(
        int userId,
        string steamId,
        AchievementSyncScope scope,
        CancellationToken cancellationToken = default);

    Task RecomputeUserAchievementStatsAsync(int userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<int>> GetPriorityCompletionAppIdsAsync(
        int userId,
        string steamId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<int>> GetCrawlCompletionAppIdsAsync(
        int userId,
        int skip,
        int take,
        CancellationToken cancellationToken = default);
}
