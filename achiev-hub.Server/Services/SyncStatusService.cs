using achiev_hub.Server.Data;
using achiev_hub.Server.DTOs;
using achiev_hub.Server.Enums;
using achiev_hub.Server.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

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

        return await BuildStatusAsync(user.Id, user.Status, user.AvgPercentage, user.AchievementSyncCoverage, cancellationToken);
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

        return await BuildStatusAsync(user.Id, user.Status, user.AvgPercentage, user.AchievementSyncCoverage, cancellationToken);
    }

    private async Task<SyncStatusDto> BuildStatusAsync(
        int userId,
        int status,
        decimal avgPercentage,
        decimal coverage,
        CancellationToken cancellationToken)
    {
        var ownedWithStats = await _db.UsersGames.AsNoTracking()
            .CountAsync(ug => ug.UserId == userId && ug.Game.HasCommunityVisibleStats == true, cancellationToken);
        var syncedWithStats = await _db.UsersGames.AsNoTracking()
            .CountAsync(
                ug => ug.UserId == userId
                    && ug.Game.HasCommunityVisibleStats == true
                    && ug.AchievementsSyncedAt != null,
                cancellationToken);

        var jobs = await _db.SyncJobs.AsNoTracking()
            .Where(j => j.UserId == userId)
            .OrderByDescending(j => j.UpdatedAt)
            .Take(10)
            .Select(j => new SyncJobStatusDto
            {
                Id = j.Id,
                Type = j.Type.ToString(),
                Status = j.Status.ToString(),
                ProgressDone = j.ProgressDone,
                ProgressTotal = j.ProgressTotal,
                LastError = j.LastError,
                UpdatedAt = j.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        var isUpdating = await _db.SyncJobs.AsNoTracking().AnyAsync(
            j => j.UserId == userId
                && (j.Status == SyncJobStatus.Pending || j.Status == SyncJobStatus.Running)
                && j.Type != SyncJobType.AchievementCrawl
                && j.Type != SyncJobType.MaintenanceNightly,
            cancellationToken);

        var libraryDone = await _db.SyncJobs.AsNoTracking().AnyAsync(
            j => j.UserId == userId && j.Type == SyncJobType.LibraryFull && j.Status == SyncJobStatus.Succeeded,
            cancellationToken);
        var priorityDone = await _db.SyncJobs.AsNoTracking().AnyAsync(
            j => j.UserId == userId && j.Type == SyncJobType.AchievementPriority && j.Status == SyncJobStatus.Succeeded,
            cancellationToken);

        var isReady = status == (int)StatusEnum.Active
            || (status == (int)StatusEnum.Provisioning && libraryDone && priorityDone);

        return new SyncStatusDto
        {
            AvgPercentage = avgPercentage,
            AchievementSyncCoverage = coverage,
            OwnedWithStats = ownedWithStats,
            SyncedWithStats = syncedWithStats,
            IsUpdating = isUpdating || status == (int)StatusEnum.Provisioning,
            IsReady = isReady && status == (int)StatusEnum.Active,
            Status = status,
            StatusLabel = ((StatusEnum)status).ToString(),
            Jobs = jobs
        };
    }
}
