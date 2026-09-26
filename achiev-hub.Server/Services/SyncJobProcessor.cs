using achiev_hub.Server.Data;
using achiev_hub.Server.Entities;
using achiev_hub.Server.Enums;
using achiev_hub.Server.Options;
using achiev_hub.Server.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace achiev_hub.Server.Services;

public class SyncJobProcessor
{
    private readonly ApplicationDbContext _db;
    private readonly ISteamSyncService _steamSyncService;
    private readonly ISyncJobEnqueueService _enqueueService;
    private readonly SteamApiThrottle _steamGate;
    private readonly SyncWorkerOptions _options;
    private readonly ILogger<SyncJobProcessor> _logger;

    public SyncJobProcessor(
        ApplicationDbContext db,
        ISteamSyncService steamSyncService,
        ISyncJobEnqueueService enqueueService,
        SteamApiThrottle steamGate,
        IOptions<SyncWorkerOptions> options,
        ILogger<SyncJobProcessor> logger)
    {
        _db = db;
        _steamSyncService = steamSyncService;
        _enqueueService = enqueueService;
        _steamGate = steamGate;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<SyncJob?> ClaimNextAsync(CancellationToken cancellationToken)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var claimedIds = await _db.Database
            .SqlQuery<int>($"""
                WITH cte AS (
                    SELECT "Id"
                    FROM sync_jobs
                    WHERE "Status" = {(int)SyncJobStatus.Pending}
                      AND "AvailableAt" <= {now}
                    ORDER BY "Type", "AvailableAt", "Id"
                    FOR UPDATE SKIP LOCKED
                    LIMIT 1
                )
                UPDATE sync_jobs AS j
                SET "Status" = {(int)SyncJobStatus.Running},
                    "UpdatedAt" = {now},
                    "Attempts" = j."Attempts" + 1
                FROM cte
                WHERE j."Id" = cte."Id"
                RETURNING j."Id"
                """)
            .ToListAsync(cancellationToken);

        if (claimedIds.Count == 0)
        {
            await tx.CommitAsync(cancellationToken);
            return null;
        }

        var job = await _db.SyncJobs.FirstAsync(j => j.Id == claimedIds[0], cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return job;
    }

    public async Task ProcessAsync(SyncJob job, CancellationToken cancellationToken)
    {
        var continueCrawl = false;
        try
        {
            await _steamGate.WaitAsync(cancellationToken);
            try
            {
                switch (job.Type)
                {
                    case SyncJobType.LibraryFull:
                        await ProcessLibraryAsync(job, LibrarySyncScope.Full, cancellationToken);
                        break;
                    case SyncJobType.LibraryRecent:
                        await ProcessLibraryAsync(job, LibrarySyncScope.Recent, cancellationToken);
                        break;
                    case SyncJobType.AchievementPriority:
                        await ProcessAchievementPriorityAsync(job, cancellationToken);
                        break;
                    case SyncJobType.AchievementCrawl:
                        continueCrawl = await ProcessAchievementCrawlAsync(job, cancellationToken);
                        break;
                    case SyncJobType.AchievementGame:
                        await ProcessAchievementGameAsync(job, cancellationToken);
                        break;
                    case SyncJobType.MaintenanceNightly:
                        await ProcessMaintenanceNightlyAsync(job, cancellationToken);
                        break;
                    default:
                        throw new InvalidOperationException($"Unknown sync job type {job.Type}");
                }
            }
            finally
            {
                _steamGate.Release();
            }

            job.Status = SyncJobStatus.Succeeded;
            job.LastError = null;
            job.UpdatedAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);

            if (continueCrawl && job.UserId is int crawlUserId && !string.IsNullOrWhiteSpace(job.SteamId))
            {
                await _enqueueService.EnqueueAsync(
                    SyncJobType.AchievementCrawl,
                    crawlUserId,
                    job.SteamId,
                    cancellationToken: cancellationToken);
            }

            if (job.UserId is int userId)
            {
                await TryActivateProvisioningUserAsync(userId, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Sync job {JobId} ({Type}) failed attempt {Attempts}", job.Id, job.Type, job.Attempts);
            job.LastError = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message;
            job.UpdatedAt = DateTimeOffset.UtcNow;

            if (job.Attempts >= _options.MaxAttempts)
            {
                job.Status = SyncJobStatus.Dead;
            }
            else
            {
                job.Status = SyncJobStatus.Pending;
                var delaySeconds = Math.Min(300, (int)Math.Pow(2, job.Attempts));
                job.AvailableAt = DateTimeOffset.UtcNow.AddSeconds(delaySeconds);
            }

            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task ProcessLibraryAsync(SyncJob job, LibrarySyncScope scope, CancellationToken cancellationToken)
    {
        if (job.UserId is not int userId || string.IsNullOrWhiteSpace(job.SteamId))
        {
            throw new InvalidOperationException("Library sync requires userId and steamId.");
        }

        await _steamSyncService.SyncLibraryAsync(
            userId,
            job.SteamId,
            scope,
            _options.MaxStoreEnrichPerLibrarySync,
            cancellationToken);

        job.ProgressDone = 1;
        job.ProgressTotal = 1;
    }

    private async Task ProcessAchievementPriorityAsync(SyncJob job, CancellationToken cancellationToken)
    {
        if (job.UserId is not int userId || string.IsNullOrWhiteSpace(job.SteamId))
        {
            throw new InvalidOperationException("Achievement priority sync requires userId and steamId.");
        }

        var appIds = await _steamSyncService.GetPriorityCompletionAppIdsAsync(userId, job.SteamId, cancellationToken);
        job.ProgressTotal = appIds.Count;
        job.ProgressDone = 0;

        foreach (var appId in appIds)
        {
            await _steamSyncService.SyncGameCompletionPercentageAsync(userId, job.SteamId, appId, cancellationToken);
            job.ProgressDone++;
            job.UpdatedAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<bool> ProcessAchievementCrawlAsync(SyncJob job, CancellationToken cancellationToken)
    {
        if (job.UserId is not int userId || string.IsNullOrWhiteSpace(job.SteamId))
        {
            throw new InvalidOperationException("Achievement crawl requires userId and steamId.");
        }

        // Always take next unsynced batch from the front (cursor unused after completion %).
        var batch = await _steamSyncService.GetCrawlCompletionAppIdsAsync(
            userId,
            0,
            _options.BatchSize,
            cancellationToken);

        job.ProgressTotal = batch.Count;
        job.ProgressDone = 0;

        foreach (var appId in batch)
        {
            await _steamSyncService.SyncGameCompletionPercentageAsync(userId, job.SteamId, appId, cancellationToken);
            job.ProgressDone++;
            job.UpdatedAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }

        return batch.Count >= _options.BatchSize;
    }

    private async Task ProcessAchievementGameAsync(SyncJob job, CancellationToken cancellationToken)
    {
        if (job.UserId is not int userId
            || string.IsNullOrWhiteSpace(job.SteamId)
            || job.AppId is not int appId)
        {
            throw new InvalidOperationException("Achievement game sync requires userId, steamId, and appId.");
        }

        await _steamSyncService.SyncGameAchievementsAsync(userId, job.SteamId, appId, cancellationToken);
        job.ProgressDone = 1;
        job.ProgressTotal = 1;
    }

    private async Task ProcessMaintenanceNightlyAsync(SyncJob job, CancellationToken cancellationToken)
    {
        var userIds = await _db.Users
            .AsNoTracking()
            .Where(u => u.Status == (int)StatusEnum.Active || u.Status == (int)StatusEnum.Provisioning)
            .Where(u => u.SteamId != null)
            .Select(u => new { u.Id, u.SteamId })
            .ToListAsync(cancellationToken);

        job.ProgressTotal = userIds.Count;
        job.ProgressDone = 0;

        foreach (var user in userIds)
        {
            await _enqueueService.EnqueueAsync(
                SyncJobType.LibraryRecent,
                user.Id,
                user.SteamId,
                cancellationToken: cancellationToken);
            await _enqueueService.EnqueueAsync(
                SyncJobType.AchievementPriority,
                user.Id,
                user.SteamId,
                cancellationToken: cancellationToken);
            await _enqueueService.EnqueueAsync(
                SyncJobType.AchievementCrawl,
                user.Id,
                user.SteamId,
                cancellationToken: cancellationToken);

            job.ProgressDone++;
            job.UpdatedAt = DateTimeOffset.UtcNow;
            if (job.ProgressDone % 25 == 0)
            {
                await _db.SaveChangesAsync(cancellationToken);
            }
        }
    }

    private async Task TryActivateProvisioningUserAsync(int userId, CancellationToken cancellationToken)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null || user.Status != (int)StatusEnum.Provisioning)
        {
            return;
        }

        var libraryDone = await _db.SyncJobs.AnyAsync(
            j => j.UserId == userId
                && j.Type == SyncJobType.LibraryFull
                && j.Status == SyncJobStatus.Succeeded,
            cancellationToken);

        var priorityDone = await _db.SyncJobs.AnyAsync(
            j => j.UserId == userId
                && j.Type == SyncJobType.AchievementPriority
                && j.Status == SyncJobStatus.Succeeded,
            cancellationToken);

        if (!libraryDone || !priorityDone)
        {
            return;
        }

        user.Status = (int)StatusEnum.Active;
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("User {UserId} activated after provisioning min-bar sync", userId);
    }
}
