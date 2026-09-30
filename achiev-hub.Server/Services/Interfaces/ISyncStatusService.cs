using achiev_hub.Server.DTOs;
using SteamSync.Shared;

namespace achiev_hub.Server.Services.Interfaces;

public interface ISyncStatusService
{
    Task<SyncStatusDto?> GetStatusForUserAsync(int userId, CancellationToken cancellationToken = default);
    Task<SyncStatusDto?> GetProvisioningStatusBySteamIdAsync(string steamId, CancellationToken cancellationToken = default);
    Task<UserSyncStatusDto?> GetUserSyncStatusAsync(int userId, CancellationToken cancellationToken = default);
}
