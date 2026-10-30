namespace achiev_hub.Server.Infrastructure.Security;

public static class SteamIdBinder
{
    /// <summary>
    /// Resolves the SteamID64 that the caller is allowed to query.
    /// Query parameter must be absent or exactly equal to the token claim.
    /// </summary>
    public static bool TryResolve(
        string? tokenSteamId,
        string? requestedSteamId,
        out string steamId,
        out string? error)
    {
        steamId = string.Empty;
        error = null;

        if (string.IsNullOrWhiteSpace(tokenSteamId))
        {
            error = "Steam ID is missing from the token.";
            return false;
        }

        var claim = tokenSteamId.Trim();
        if (!string.IsNullOrWhiteSpace(requestedSteamId)
            && !string.Equals(requestedSteamId.Trim(), claim, StringComparison.Ordinal))
        {
            error = "steamId does not match the authenticated Steam account.";
            return false;
        }

        steamId = claim;
        return true;
    }
}
