namespace achiev_hub.Server.Services.Interfaces;

public interface ISteamVisibilityService
{
    Task<(bool IsLibraryPublic, string? PersonaName, string? Avatar)> ProbeAsync(
        string steamId,
        CancellationToken cancellationToken = default);

    /// <summary>Fetches Steam summary, updates user.SteamLibraryPublic, returns whether library is public.</summary>
    Task<bool> RefreshUserSteamVisibilityAsync(
        int userId,
        string steamId,
        CancellationToken cancellationToken = default);
}
