using SteamSync.Shared.Messages;

namespace achiev_hub.Server.Services.Interfaces;

/// <summary>Publishes Steam sync jobs to RabbitMQ for the steam-sync worker.</summary>
public interface ISyncJobEnqueueService
{
    Task<Guid> EnqueueAsync(
        string jobType,
        int userId,
        string steamId,
        int? appId = null,
        string priority = "medium",
        bool includeCrawl = false,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Guid>> EnqueueRegisterSyncAsync(
        int userId,
        string steamId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Guid>> EnqueueLoginSyncAsync(
        int userId,
        string steamId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Guid>> EnqueueManualLibrarySyncAsync(
        int userId,
        string steamId,
        CancellationToken cancellationToken = default);

    Task<Guid> EnqueueGameAchievementSyncAsync(
        int userId,
        string steamId,
        int appId,
        CancellationToken cancellationToken = default);
}
