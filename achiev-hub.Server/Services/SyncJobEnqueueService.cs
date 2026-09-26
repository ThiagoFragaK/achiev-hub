using achiev_hub.Server.Data;
using achiev_hub.Server.Entities;
using achiev_hub.Server.Enums;
using achiev_hub.Server.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace achiev_hub.Server.Services;

public class SyncJobEnqueueService : ISyncJobEnqueueService
{
    private readonly ApplicationDbContext _db;

    public SyncJobEnqueueService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<int> EnqueueAsync(
        SyncJobType type,
        int? userId,
        string? steamId,
        int? appId = null,
        CancellationToken cancellationToken = default)
    {
        if (userId.HasValue && type != SyncJobType.MaintenanceNightly && type != SyncJobType.AchievementGame)
        {
            var existing = await _db.SyncJobs
                .Where(j => j.UserId == userId
                    && j.Type == type
                    && (j.Status == SyncJobStatus.Pending || j.Status == SyncJobStatus.Running)
                    && (appId == null || j.AppId == appId))
                .Select(j => (int?)j.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (existing is int id)
            {
                return id;
            }
        }

        if (type == SyncJobType.AchievementGame && userId.HasValue && appId.HasValue)
        {
            var existingGameJob = await _db.SyncJobs
                .Where(j => j.UserId == userId
                    && j.Type == SyncJobType.AchievementGame
                    && j.AppId == appId
                    && (j.Status == SyncJobStatus.Pending || j.Status == SyncJobStatus.Running))
                .Select(j => (int?)j.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (existingGameJob is int gameJobId)
            {
                return gameJobId;
            }
        }

        var now = DateTimeOffset.UtcNow;
        var job = new SyncJob
        {
            UserId = userId,
            SteamId = steamId,
            Type = type,
            AppId = appId,
            Status = SyncJobStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now,
            AvailableAt = now
        };

        _db.SyncJobs.Add(job);
        await _db.SaveChangesAsync(cancellationToken);
        return job.Id;
    }

    public async Task<IReadOnlyList<int>> EnqueueRegisterSyncAsync(
        int userId,
        string steamId,
        CancellationToken cancellationToken = default)
    {
        var ids = new List<int>
        {
            await EnqueueAsync(SyncJobType.LibraryFull, userId, steamId, cancellationToken: cancellationToken),
            await EnqueueAsync(SyncJobType.AchievementPriority, userId, steamId, cancellationToken: cancellationToken),
            await EnqueueAsync(SyncJobType.AchievementCrawl, userId, steamId, cancellationToken: cancellationToken)
        };
        return ids;
    }

    public async Task<IReadOnlyList<int>> EnqueueLoginSyncAsync(
        int userId,
        string steamId,
        CancellationToken cancellationToken = default)
    {
        var ids = new List<int>
        {
            await EnqueueAsync(SyncJobType.LibraryRecent, userId, steamId, cancellationToken: cancellationToken),
            await EnqueueAsync(SyncJobType.AchievementPriority, userId, steamId, cancellationToken: cancellationToken)
        };
        return ids;
    }

    public async Task<IReadOnlyList<int>> EnqueueManualLibrarySyncAsync(
        int userId,
        string steamId,
        CancellationToken cancellationToken = default)
    {
        return await EnqueueRegisterSyncAsync(userId, steamId, cancellationToken);
    }
}
