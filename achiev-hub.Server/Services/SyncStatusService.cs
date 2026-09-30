using achiev_hub.Server.Data;
using achiev_hub.Server.DTOs;
using achiev_hub.Server.Enums;
using achiev_hub.Server.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using SteamSync.Shared;
using System.Collections.Concurrent;

namespace achiev_hub.Server.Services;

public class SyncStatusService : ISyncStatusService
{
    private static readonly ConcurrentDictionary<int, long> DebugLastLogMs = new();
    private readonly ApplicationDbContext _db;

    public SyncStatusService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<SyncStatusDto?> GetStatusForUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        return await BuildStatusAsync(
            user.Id,
            user.Status,
            user.AvgPercentage,
            user.AchievementSyncCoverage,
            user.SteamLibraryPublic,
            cancellationToken);
    }

    public async Task<SyncStatusDto?> GetProvisioningStatusBySteamIdAsync(
        string steamId,
        CancellationToken cancellationToken = default)
    {
        var normalized = steamId.Trim();
        var user = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.SteamId == normalized, cancellationToken);
        if (user is null)
        {
            return null;
        }

        return await BuildStatusAsync(
            user.Id,
            user.Status,
            user.AvgPercentage,
            user.AchievementSyncCoverage,
            user.SteamLibraryPublic,
            cancellationToken);
    }

    public async Task<UserSyncStatusDto?> GetUserSyncStatusAsync(int userId, CancellationToken cancellationToken = default)
    {
        var row = await _db.UserSyncStatuses.AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);
        if (row is null)
        {
            var userExists = await _db.Users.AsNoTracking().AnyAsync(u => u.Id == userId, cancellationToken);
            if (!userExists)
            {
                return null;
            }

            return new UserSyncStatusDto
            {
                UserId = userId,
                Status = SyncStatus.Pending
            };
        }

        return new UserSyncStatusDto
        {
            UserId = row.UserId,
            LastPartialSync = row.LastPartialSync,
            LastFullSync = row.LastFullSync,
            SyncProgressPercent = row.SyncProgressPercent,
            GamesSyncedCount = row.GamesSyncedCount,
            TotalGamesCount = row.TotalGamesCount,
            Status = row.Status,
            LastError = row.LastError,
            LastJobId = row.LastJobId
        };
    }

    private async Task<SyncStatusDto> BuildStatusAsync(
        int userId,
        int status,
        decimal avgPercentage,
        decimal coverage,
        bool steamLibraryPublic,
        CancellationToken cancellationToken)
    {
        var ownedWithStats = await _db.UsersGames.AsNoTracking()
            .CountAsync(ug => ug.UserId == userId && ug.Game.HasCommunityVisibleStats == true, cancellationToken);
        var syncedWithStats = await _db.UsersGames.AsNoTracking()
            .CountAsync(
                ug => ug.UserId == userId
                    && ug.Game.HasCommunityVisibleStats == true
                    && ug.AchievementsSyncedAt != null
                    && !ug.AchievementSyncUnavailable,
                cancellationToken);

        var syncRow = await _db.UserSyncStatuses.AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);

        var jobs = new List<SyncJobStatusDto>();
        if (syncRow is not null)
        {
            jobs.Add(new SyncJobStatusDto
            {
                Id = syncRow.LastJobId?.GetHashCode() ?? 0,
                Type = "user_sync",
                Status = syncRow.Status.ToString(),
                ProgressDone = syncRow.GamesSyncedCount,
                ProgressTotal = syncRow.TotalGamesCount,
                LastError = syncRow.LastError,
                UpdatedAt = syncRow.UpdatedAt
            });
        }

        var isUpdating = steamLibraryPublic && syncRow is not null
            && (syncRow.Status == SyncStatus.Pending || syncRow.Status == SyncStatus.Syncing);

        var isPartial = syncRow?.Status == SyncStatus.Partial;
        var isReady = status == (int)StatusEnum.Active;

        var syncSummary = syncRow is null
            ? SyncSummaryDto.Empty
            : SyncSummaryDto.From(
                syncRow.Status,
                syncRow.LastFullSync,
                syncRow.LastPartialSync,
                syncRow.GamesSyncedCount > 0 ? syncRow.GamesSyncedCount : syncedWithStats,
                syncRow.TotalGamesCount > 0 ? syncRow.TotalGamesCount : ownedWithStats,
                syncRow.SyncProgressPercent);

        var dto = new SyncStatusDto
        {
            AvgPercentage = avgPercentage,
            AchievementSyncCoverage = coverage,
            OwnedWithStats = ownedWithStats,
            SyncedWithStats = syncedWithStats,
            IsUpdating = isUpdating,
            IsReady = isReady,
            IsPartial = isPartial,
            SteamLibraryPublic = steamLibraryPublic,
            Status = status,
            StatusLabel = ((StatusEnum)status).ToString(),
            Jobs = jobs,
            SyncProgressPercent = syncRow?.SyncProgressPercent ?? 0,
            LastJobId = syncRow?.LastJobId,
            Sync = syncSummary
        };
        // #region agent log
        try
        {
            var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var prev = DebugLastLogMs.GetOrAdd(userId, 0);
            if (nowMs - prev >= 10000)
            {
                DebugLastLogMs[userId] = nowMs;
                System.IO.File.AppendAllText(@"K:\Projekten\MyApps\achiev-hub\debug-321fb6.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "321fb6", runId = "pre-fix", hypothesisId = "E", location = "SyncStatusService.cs:BuildStatus", message = "Sync status snapshot", data = new { userId, syncStatus = syncRow?.Status.ToString(), syncRow?.SyncProgressPercent, syncRow?.GamesSyncedCount, syncRow?.TotalGamesCount, ownedWithStats, syncedWithStats, avgPercentage, coverage, isUpdating, isPartial, steamLibraryPublic, syncRow?.LastError, syncRow?.LastJobId, syncRow?.LockedUntil, syncRow?.UpdatedAt }, timestamp = nowMs }) + "\n");
            }
        }
        catch { }
        // #endregion
        return dto;
    }
}
