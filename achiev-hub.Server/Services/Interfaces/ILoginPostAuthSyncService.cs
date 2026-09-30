namespace achiev_hub.Server.Services.Interfaces;

public interface ILoginPostAuthSyncService
{
    Task RunAsync(int userId, string steamId, CancellationToken cancellationToken = default);
}
