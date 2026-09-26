using achiev_hub.Server.Enums;

namespace achiev_hub.Server.Services.Interfaces;

public interface ISyncJobEnqueueService
{
    Task<int> EnqueueAsync(
        SyncJobType type,
        int? userId,
        string? steamId,
        int? appId = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<int>> EnqueueRegisterSyncAsync(
        int userId,
        string steamId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<int>> EnqueueLoginSyncAsync(
        int userId,
        string steamId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<int>> EnqueueManualLibrarySyncAsync(
        int userId,
        string steamId,
        CancellationToken cancellationToken = default);
}
