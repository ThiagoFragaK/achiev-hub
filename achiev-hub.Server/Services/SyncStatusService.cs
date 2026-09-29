using achiev_hub.Server.Data;
using achiev_hub.Server.DTOs;
using achiev_hub.Server.Enums;
using achiev_hub.Server.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using SteamSync.Shared;

namespace achiev_hub.Server.Services;

public class SyncStatusService : ISyncStatusService
{
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

        if (steamLibraryPublic && status == (int)StatusEnum.Provisioning)
        {
            isUpdating = true;
        }

        var fullDone = syncRow?.LastFullSync is not null && syncRow.Status == SyncStatus.Complete;
        var isReady = status == (int)StatusEnum.Active
            || (status == (int)StatusEnum.Provisioning && fullDone);

        return new SyncStatusDto
        {
            AvgPercentage = avgPercentage,
            AchievementSyncCoverage = coverage,
            OwnedWithStats = ownedWithStats,
            SyncedWithStats = syncedWithStats,
            IsUpdating = isUpdating,
            IsReady = isReady && status == (int)StatusEnum.Active,
            SteamLibraryPublic = steamLibraryPublic,
            Status = status,
            StatusLabel = ((StatusEnum)status).ToString(),
            Jobs = jobs,
            SyncProgressPercent = syncRow?.SyncProgressPercent ?? 0,
            LastJobId = syncRow?.LastJobId
        };
    }
}
