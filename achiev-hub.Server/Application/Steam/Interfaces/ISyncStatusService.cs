using achiev_hub.Server.Application.Steam;

namespace achiev_hub.Server.Application.Steam.Interfaces;

public interface ISyncStatusService
{
    Task<SyncStatusDto?> GetStatusForUserAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task<SyncStatusDto?> GetProvisioningStatusBySteamIdAsync(
        string steamId,
        CancellationToken cancellationToken = default);
}
