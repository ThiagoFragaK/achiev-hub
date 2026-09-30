namespace achiev_hub.Server.Services.Interfaces;

public interface ISteamSnapshotService
{
    /// <summary>
    /// Fetches owned + recently played (and uses prior summary from register) and
    /// persists a lightweight users_games shell. Max ~2 Steam calls with 5s timeout each
    /// (player summary is already fetched by registration).
    /// </summary>
    Task PersistRegistrationSnapshotAsync(
        int userId,
        string steamId,
        CancellationToken cancellationToken = default);
}
