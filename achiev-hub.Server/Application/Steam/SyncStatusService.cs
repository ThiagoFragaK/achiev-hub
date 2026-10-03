using achiev_hub.Server.Application.Steam.Interfaces;
using achiev_hub.Server.Domain.Entities;
using achiev_hub.Server.Domain.Enums;
using achiev_hub.Server.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using SteamSync.Shared;

namespace achiev_hub.Server.Application.Steam;

public class SyncStatusService : ISyncStatusService
{
    private readonly ApplicationDbContext _db;

    public SyncStatusService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<SyncStatusDto?> GetStatusForUserAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var syncRow = await _db.UserSyncStatuses.AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == user.Id, cancellationToken);

        return BuildStatus(user, syncRow);
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

        var syncRow = await _db.UserSyncStatuses.AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == user.Id, cancellationToken);

        return BuildStatus(user, syncRow);
    }

    private static SyncStatusDto BuildStatus(User user, UserSyncStatus? syncRow)
    {
        var sync = syncRow is null
            ? SyncSummaryDto.Empty
            : SyncSummaryDto.From(
                syncRow.Status,
                syncRow.LastFullSync,
                syncRow.LastPartialSync,
                syncRow.GamesSyncedCount,
                syncRow.TotalGamesCount,
                syncRow.SyncProgressPercent,
                syncRow.LastError,
                syncRow.LastJobId,
                enqueued: true,
                stage: syncRow.PipelineStage);

        var isUpdating = syncRow is not null
            && (syncRow.Status == SyncStatus.Pending || syncRow.Status == SyncStatus.Syncing);

        var firstSyncPhaseDone = syncRow is not null
            && syncRow.PipelineStage is PipelineStage.FullLibrary
                or PipelineStage.FullAchievements
                or PipelineStage.Done;

        var isReady = user.Status == (int)StatusEnum.Active || firstSyncPhaseDone;

        return new SyncStatusDto
        {
            Status = user.Status,
            StatusLabel = ((StatusEnum)user.Status).ToString(),
            IsReady = isReady,
            IsUpdating = isUpdating,
            Sync = sync
        };
    }
}
