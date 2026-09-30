using achiev_hub.Server.Data;
using achiev_hub.Server.Entities;
using achiev_hub.Server.Repositories.Interfaces;
using achiev_hub.Server.Services.Interfaces;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using SteamSync.Shared;
using SteamSync.Shared.Messages;

namespace achiev_hub.Server.Services;

/// <summary>Marks <see cref="UserSyncStatus"/> pending and sends <see cref="UserSyncJob"/> to RabbitMQ.</summary>
public class SyncJobEnqueueService : ISyncJobEnqueueService
{
    public static readonly TimeSpan LockTtl = TimeSpan.FromMinutes(30);
    public static readonly TimeSpan AutoCooldown = TimeSpan.FromDays(1);
    public const int MaxManualPerDay = 2;
    public static readonly TimeSpan StaleFullSyncAge = TimeSpan.FromDays(14);

    private readonly ApplicationDbContext _db;
    private readonly ISendEndpointProvider _sendEndpointProvider;
    private readonly ISteamRepository _steamRepository;
    private readonly ILogger<SyncJobEnqueueService> _logger;
    private readonly Uri _queueUri;

    public SyncJobEnqueueService(
        ApplicationDbContext db,
        ISendEndpointProvider sendEndpointProvider,
        ISteamRepository steamRepository,
        ILogger<SyncJobEnqueueService> logger)
    {
        _db = db;
        _sendEndpointProvider = sendEndpointProvider;
        _steamRepository = steamRepository;
        _logger = logger;
        _queueUri = new Uri($"queue:{SyncQueueNames.Jobs}");
    }

    public async Task<SyncEnqueueResult> EnqueueAsync(
        string jobType,
        int userId,
        string steamId,
        SyncEnqueueKind kind = SyncEnqueueKind.Auto,
        int? appId = null,
        string priority = "medium",
        bool includeCrawl = false,
        string? scope = null,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var row = await GetOrCreateStatusAsync(userId, cancellationToken);

        if (row.Status == SyncStatus.Syncing
            || (row.LockedUntil is DateTimeOffset locked && locked > now))
        {
            // #region agent log
            try { System.IO.File.AppendAllText(@"K:\Projekten\MyApps\achiev-hub\debug-321fb6.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "321fb6", runId = "pre-fix", hypothesisId = "A", location = "SyncJobEnqueueService.cs:skip-inflight", message = "Enqueue skipped: in-flight/lock", data = new { userId, jobType, kind = kind.ToString(), status = row.Status.ToString(), row.LockedUntil, row.LastJobId }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
            // #endregion
            return SyncEnqueueResult.Skipped("Sync already in-flight for this user.");
        }

        if (kind == SyncEnqueueKind.Auto
            && row.LastAutoEnqueueAt is DateTimeOffset lastAuto
            && now - lastAuto < AutoCooldown)
        {
            // #region agent log
            try { System.IO.File.AppendAllText(@"K:\Projekten\MyApps\achiev-hub\debug-321fb6.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "321fb6", runId = "pre-fix", hypothesisId = "A", location = "SyncJobEnqueueService.cs:skip-cooldown", message = "Enqueue skipped: auto cooldown", data = new { userId, jobType, lastAuto, AutoCooldownHours = AutoCooldown.TotalHours }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
            // #endregion
            return SyncEnqueueResult.Skipped("Auto sync cooldown (max 1 per day).", rateLimited: true);
        }

        if (kind == SyncEnqueueKind.Manual)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            if (row.ManualEnqueueDate == today && row.ManualEnqueueCount >= MaxManualPerDay)
            {
                // #region agent log
                try { System.IO.File.AppendAllText(@"K:\Projekten\MyApps\achiev-hub\debug-321fb6.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "321fb6", runId = "pre-fix", hypothesisId = "A", location = "SyncJobEnqueueService.cs:skip-manual-limit", message = "Enqueue skipped: manual limit", data = new { userId, jobType, row.ManualEnqueueCount }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
                // #endregion
                return SyncEnqueueResult.Skipped(
                    "Manual sync limit reached (max 2 per day).",
                    rateLimited: true);
            }
        }

        var job = new UserSyncJob
        {
            JobType = jobType,
            Payload = new UserSyncPayload
            {
                UserId = userId,
                SteamId = steamId,
                Priority = priority,
                AppId = appId,
                IncludeCrawl = includeCrawl,
                Scope = scope
            }
        };

        row.Status = SyncStatus.Pending;
        row.LastJobId = job.JobId;
        row.LastError = null;
        row.SyncProgressPercent = 0;
        row.LockedUntil = now.Add(LockTtl);
        row.UpdatedAt = now;

        if (kind is SyncEnqueueKind.Auto or SyncEnqueueKind.Register or SyncEnqueueKind.ContinueIncomplete)
        {
            row.LastAutoEnqueueAt = now;
        }

        if (kind == SyncEnqueueKind.Manual)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            if (row.ManualEnqueueDate != today)
            {
                row.ManualEnqueueDate = today;
                row.ManualEnqueueCount = 1;
            }
            else
            {
                row.ManualEnqueueCount++;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        try
        {
            var endpoint = await _sendEndpointProvider.GetSendEndpoint(_queueUri);
            await endpoint.Send(job, cancellationToken);
        }
        catch
        {
            row.LockedUntil = null;
            row.Status = SyncStatus.Failed;
            row.LastError = "Failed to publish sync job.";
            row.UpdatedAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync(CancellationToken.None);
            throw;
        }

        _logger.LogInformation(
            "Published sync job {JobId} type={JobType} scope={Scope} user={UserId} kind={Kind}",
            job.JobId,
            jobType,
            scope,
            userId,
            kind);
        // #region agent log
        try { System.IO.File.AppendAllText(@"K:\Projekten\MyApps\achiev-hub\debug-321fb6.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "321fb6", runId = "pre-fix", hypothesisId = "B", location = "SyncJobEnqueueService.cs:published", message = "Sync job published to RabbitMQ", data = new { jobId = job.JobId, jobType, scope, userId, kind = kind.ToString(), appId, includeCrawl }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
        // #endregion

        return SyncEnqueueResult.Ok(job.JobId);
    }

    public async Task<IReadOnlyList<Guid>> EnqueueRegisterSyncAsync(
        int userId,
        string steamId,
        CancellationToken cancellationToken = default)
    {
        var result = await EnqueueAsync(
            SyncJobTypes.UserSync,
            userId,
            steamId,
            SyncEnqueueKind.Register,
            priority: "high",
            scope: SyncJobScopes.Initial,
            cancellationToken: cancellationToken);

        return result.JobId is Guid id ? [id] : [];
    }

    public async Task<IReadOnlyList<Guid>> EnqueueLoginSyncAsync(
        int userId,
        string steamId,
        CancellationToken cancellationToken = default)
    {
        var ids = new List<Guid>();
        var row = await _db.UserSyncStatuses.AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);

        // Never fully synced: first pass is Initial; after Partial, continue with full crawl.
        if (row?.LastFullSync is null)
        {
            var needsInitial = row?.LastPartialSync is null && row?.Status != SyncStatus.Partial;
            if (needsInitial)
            {
                var initial = await EnqueueAsync(
                    SyncJobTypes.UserSync,
                    userId,
                    steamId,
                    SyncEnqueueKind.Auto,
                    priority: "high",
                    scope: SyncJobScopes.Initial,
                    cancellationToken: cancellationToken);
                if (initial.JobId is Guid id)
                {
                    ids.Add(id);
                }

                return ids;
            }

            // #region agent log
            try { System.IO.File.AppendAllText(@"K:\Projekten\MyApps\achiev-hub\debug-321fb6.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "321fb6", runId = "post-fix", hypothesisId = "E", location = "SyncJobEnqueueService.cs:EnqueueLoginSyncAsync:continue", message = "Login continuing Partial with FullLibraryResync", data = new { userId, status = row?.Status.ToString(), row?.LastPartialSync }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
            // #endregion

            var continueFull = await EnqueueAsync(
                SyncJobTypes.FullLibraryResync,
                userId,
                steamId,
                SyncEnqueueKind.ContinueIncomplete,
                priority: "medium",
                cancellationToken: cancellationToken);
            if (continueFull.JobId is Guid continueId)
            {
                ids.Add(continueId);
            }

            return ids;
        }

        // Full sync older than 14 days → low-priority full resync.
        if (DateTime.UtcNow - row.LastFullSync.Value > StaleFullSyncAge)
        {
            var full = await EnqueueAsync(
                SyncJobTypes.FullLibraryResync,
                userId,
                steamId,
                SyncEnqueueKind.Auto,
                priority: "low",
                cancellationToken: cancellationToken);
            if (full.JobId is Guid id)
            {
                ids.Add(id);
            }
        }

        // Recent activity since last partial → high-priority recent job.
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(5));
            var recent = await _steamRepository.GetRecentlyPlayedGamesAsync(steamId, cts.Token);
            var lastPartial = row.LastPartialSync ?? row.LastFullSync;
            var lastPartialUnix = lastPartial.HasValue
                ? new DateTimeOffset(DateTime.SpecifyKind(lastPartial.Value, DateTimeKind.Utc)).ToUnixTimeSeconds()
                : 0L;

            var hasNewerPlay = recent.Any(g =>
                g.Playtime2WeeksMinutes > 0
                && (g.PlaytimeForeverMinutes > 0 || lastPartialUnix == 0));

            // Prefer last-played when available on owned overlay; recent list may lack rtime —
            // any 2-week playtime after a partial older than a few minutes is enough signal.
            if (!hasNewerPlay && lastPartial.HasValue)
            {
                hasNewerPlay = recent.Count > 0
                    && DateTime.UtcNow - lastPartial.Value > TimeSpan.FromHours(1);
            }

            if (hasNewerPlay)
            {
                var recentJob = await EnqueueAsync(
                    SyncJobTypes.RecentActivityOnly,
                    userId,
                    steamId,
                    SyncEnqueueKind.Auto,
                    priority: "high",
                    cancellationToken: cancellationToken);
                if (recentJob.JobId is Guid id)
                {
                    ids.Add(id);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Login recent-activity check failed for user {UserId}", userId);
        }

        return ids;
    }

    public Task<SyncEnqueueResult> EnqueueLazyRefreshAsync(
        int userId,
        string steamId,
        CancellationToken cancellationToken = default) =>
        EnqueueAsync(
            SyncJobTypes.RecentActivityOnly,
            userId,
            steamId,
            SyncEnqueueKind.Auto,
            priority: "medium",
            cancellationToken: cancellationToken);

    public Task<SyncEnqueueResult> EnqueueManualLibrarySyncAsync(
        int userId,
        string steamId,
        CancellationToken cancellationToken = default) =>
        EnqueueAsync(
            SyncJobTypes.FullLibraryResync,
            userId,
            steamId,
            SyncEnqueueKind.Manual,
            priority: "high",
            cancellationToken: cancellationToken);

    public Task<SyncEnqueueResult> EnqueueGameAchievementSyncAsync(
        int userId,
        string steamId,
        int appId,
        CancellationToken cancellationToken = default) =>
        EnqueueAsync(
            SyncJobTypes.UserSync,
            userId,
            steamId,
            SyncEnqueueKind.Manual,
            appId,
            priority: "high",
            cancellationToken: cancellationToken);

    private async Task<UserSyncStatus> GetOrCreateStatusAsync(int userId, CancellationToken cancellationToken)
    {
        var row = await _db.UserSyncStatuses.FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);
        if (row is not null)
        {
            return row;
        }

        row = new UserSyncStatus { UserId = userId };
        _db.UserSyncStatuses.Add(row);
        await _db.SaveChangesAsync(cancellationToken);
        return row;
    }
}
