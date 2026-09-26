using System.Text.RegularExpressions;

namespace achiev_hub.Server.Support;

public static partial class SteamIdValidator
{
    public static bool IsValidSteamId64(string? steamId)
    {
        return !string.IsNullOrWhiteSpace(steamId) && SteamId64Regex().IsMatch(steamId.Trim());
    }

    [GeneratedRegex(@"^\d{17}$", RegexOptions.CultureInvariant)]
    private static partial Regex SteamId64Regex();
}
