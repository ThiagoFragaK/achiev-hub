using achiev_hub.Server.Services;

namespace achiev_hub.Server.Services.Interfaces;

public interface ISteamSyncService
{
    Task SyncLibraryAsync(
        int userId,
        string steamId,
        LibrarySyncScope scope = LibrarySyncScope.Full,
        CancellationToken cancellationToken = default);

    Task SyncGameAchievementsAsync(int userId, string steamId, int appId, CancellationToken cancellationToken = default);

    Task SyncAchievementsForUserAsync(
        int userId,
        string steamId,
        AchievementSyncScope scope,
        CancellationToken cancellationToken = default);
}
