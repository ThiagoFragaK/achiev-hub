namespace achiev_hub.Server.Application.Steam.Interfaces;

public interface ISteamSyncClient
{
    Task PublishFirstSyncAsync(int userId, string steamId, CancellationToken cancellationToken = default);

    Task<Guid> PublishSyncLibraryAsync(int userId, string steamId, CancellationToken cancellationToken = default);
}
