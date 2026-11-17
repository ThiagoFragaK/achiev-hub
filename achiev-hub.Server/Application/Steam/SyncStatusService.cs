using achiev_hub.Server.Application.Steam.Interfaces;
using achiev_hub.Server.Application.Users.Interfaces;
using achiev_hub.Server.Domain.Entities;
using achiev_hub.Server.Domain.Enums;
using achiev_hub.Server.Infrastructure.Steam.Contracts;

namespace achiev_hub.Server.Application.Steam;

public class SyncStatusService : ISyncStatusService
{
    private readonly IUserRepository _users;
    private readonly IUserSyncStatusRepository _syncStatuses;

    public SyncStatusService(IUserRepository users, IUserSyncStatusRepository syncStatuses)
    {
        _users = users;
        _syncStatuses = syncStatuses;
    }

    public async Task<SyncStatusDto?> GetStatusForUserAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var syncRow = await _syncStatuses.GetByUserIdAsync(user.Id, cancellationToken);
        return BuildStatus(user, syncRow);
    }

    public async Task<SyncStatusDto?> GetProvisioningStatusBySteamIdAsync(
        string steamId,
        CancellationToken cancellationToken = default)
    {
        var normalized = steamId.Trim();
        var user = await _users.GetBySteamIdAsync(normalized, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var syncRow = await _syncStatuses.GetByUserIdAsync(user.Id, cancellationToken);
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
