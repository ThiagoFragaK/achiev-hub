using SteamSync.Shared;
using SteamSync.Shared.Messages;

namespace achiev_hub.Server.Services.Interfaces;

public enum SyncEnqueueKind
{
    Auto,
    Manual,
    /// <summary>Registration initial enqueue — bypasses auto daily cooldown but still respects in-flight lock.</summary>
    Register,
    /// <summary>Continue an incomplete Partial sync — bypasses auto daily cooldown but still respects in-flight lock.</summary>
    ContinueIncomplete
}

public sealed class SyncEnqueueResult
{
    public bool Enqueued { get; init; }
    public Guid? JobId { get; init; }
    public string? SkipReason { get; init; }
    public bool RateLimited { get; init; }

    public static SyncEnqueueResult Skipped(string reason, bool rateLimited = false) =>
        new() { Enqueued = false, SkipReason = reason, RateLimited = rateLimited };

    public static SyncEnqueueResult Ok(Guid jobId) =>
        new() { Enqueued = true, JobId = jobId };
}

/// <summary>Publishes Steam sync jobs to RabbitMQ for the steam-sync worker.</summary>
public interface ISyncJobEnqueueService
{
    Task<SyncEnqueueResult> EnqueueAsync(
        string jobType,
        int userId,
        string steamId,
        SyncEnqueueKind kind = SyncEnqueueKind.Auto,
        int? appId = null,
        string priority = "medium",
        bool includeCrawl = false,
        string? scope = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Guid>> EnqueueRegisterSyncAsync(
        int userId,
        string steamId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Smart login enqueue: initial if never fully synced, full resync if stale,
    /// and/or recent activity when games were played since last partial.
    /// </summary>
    Task<IReadOnlyList<Guid>> EnqueueLoginSyncAsync(
        int userId,
        string steamId,
        CancellationToken cancellationToken = default);

    Task<SyncEnqueueResult> EnqueueLazyRefreshAsync(
        int userId,
        string steamId,
        CancellationToken cancellationToken = default);

    Task<SyncEnqueueResult> EnqueueManualLibrarySyncAsync(
        int userId,
        string steamId,
        CancellationToken cancellationToken = default);

    Task<SyncEnqueueResult> EnqueueGameAchievementSyncAsync(
        int userId,
        string steamId,
        int appId,
        CancellationToken cancellationToken = default);
}
