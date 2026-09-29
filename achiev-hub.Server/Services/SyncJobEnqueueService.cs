using achiev_hub.Server.Data;
using achiev_hub.Server.Entities;
using achiev_hub.Server.Services.Interfaces;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using SteamSync.Shared;
using SteamSync.Shared.Messages;

namespace achiev_hub.Server.Services;

/// <summary>Marks <see cref="UserSyncStatus"/> pending and sends <see cref="UserSyncJob"/> to RabbitMQ.</summary>
public class SyncJobEnqueueService : ISyncJobEnqueueService
{
    private readonly ApplicationDbContext _db;
    private readonly ISendEndpointProvider _sendEndpointProvider;
    private readonly ILogger<SyncJobEnqueueService> _logger;
    private readonly Uri _queueUri;

    public SyncJobEnqueueService(
        ApplicationDbContext db,
        ISendEndpointProvider sendEndpointProvider,
        ILogger<SyncJobEnqueueService> logger)
    {
        _db = db;
        _sendEndpointProvider = sendEndpointProvider;
        _logger = logger;
        _queueUri = new Uri($"queue:{SyncQueueNames.Jobs}");
    }

    public async Task<Guid> EnqueueAsync(
        string jobType,
        int userId,
        string steamId,
        int? appId = null,
        string priority = "medium",
        bool includeCrawl = false,
        CancellationToken cancellationToken = default)
    {
        var job = new UserSyncJob
        {
            JobType = jobType,
            Payload = new UserSyncPayload
            {
                UserId = userId,
                SteamId = steamId,
                Priority = priority,
                AppId = appId,
                IncludeCrawl = includeCrawl
            }
        };

        await UpsertPendingStatusAsync(userId, job.JobId, cancellationToken);

        var endpoint = await _sendEndpointProvider.GetSendEndpoint(_queueUri);
        await endpoint.Send(job, cancellationToken);

        _logger.LogInformation(
            "Published sync job {JobId} type={JobType} user={UserId}",
            job.JobId,
            jobType,
            userId);

        return job.JobId;
    }

    public async Task<IReadOnlyList<Guid>> EnqueueRegisterSyncAsync(
        int userId,
        string steamId,
        CancellationToken cancellationToken = default)
    {
        var id = await EnqueueAsync(
            SyncJobTypes.FullLibraryResync,
            userId,
            steamId,
            priority: "high",
            cancellationToken: cancellationToken);
        return [id];
    }

    public async Task<IReadOnlyList<Guid>> EnqueueLoginSyncAsync(
        int userId,
        string steamId,
        CancellationToken cancellationToken = default)
    {
        var id = await EnqueueAsync(
            SyncJobTypes.RecentActivityOnly,
            userId,
            steamId,
            priority: "medium",
            cancellationToken: cancellationToken);
        return [id];
    }

    public Task<IReadOnlyList<Guid>> EnqueueManualLibrarySyncAsync(
        int userId,
        string steamId,
        CancellationToken cancellationToken = default) =>
        EnqueueRegisterSyncAsync(userId, steamId, cancellationToken);

    public Task<Guid> EnqueueGameAchievementSyncAsync(
        int userId,
        string steamId,
        int appId,
        CancellationToken cancellationToken = default) =>
        EnqueueAsync(
            SyncJobTypes.UserSync,
            userId,
            steamId,
            appId,
            priority: "high",
            cancellationToken: cancellationToken);

    private async Task UpsertPendingStatusAsync(int userId, Guid jobId, CancellationToken cancellationToken)
    {
        var row = await _db.UserSyncStatuses.FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);
        if (row is null)
        {
            row = new UserSyncStatus { UserId = userId };
            _db.UserSyncStatuses.Add(row);
        }

        row.Status = SyncStatus.Pending;
        row.LastJobId = jobId;
        row.LastError = null;
        row.SyncProgressPercent = 0;
        row.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }
}
