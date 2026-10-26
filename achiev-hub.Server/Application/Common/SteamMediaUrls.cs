using System.Text.RegularExpressions;

namespace achiev_hub.Server.Application.Common;

/// <summary>
/// Steam CDN helpers: persist 40-char content hashes; rebuild absolute URLs at read time.
/// </summary>
public static partial class SteamMediaUrls
{
    private const string AppIconCdn =
        "https://media.steampowered.com/steamcommunity/public/images/apps";

    private const string HeaderCdn =
        "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps";

    [GeneratedRegex(@"([0-9a-f]{40})(?:\.jpe?g)?(?:\?.*)?$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex HashTailRegex();

    /// <summary>Extracts a 40-char hex hash from a Steam CDN URL, or returns a trimmed hash as-is.</summary>
    public static string? ToStorageHash(string? hashOrUrl)
    {
        if (string.IsNullOrWhiteSpace(hashOrUrl))
        {
            return null;
        }

        var value = hashOrUrl.Trim();
        if (!value.Contains("://", StringComparison.Ordinal))
        {
            return value.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
                || value.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
                ? Path.GetFileNameWithoutExtension(value)
                : value;
        }

        var match = HashTailRegex().Match(value);
        return match.Success ? match.Groups[1].Value : value;
    }

    public static string? AppIconUrl(string? appId, string? hashOrUrl)
    {
        if (string.IsNullOrWhiteSpace(hashOrUrl))
        {
            return null;
        }

        var value = hashOrUrl.Trim();
        if (value.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return value;
        }

        if (string.IsNullOrWhiteSpace(appId))
        {
            return value;
        }

        var hash = ToStorageHash(value) ?? value;
        return $"{AppIconCdn}/{appId.Trim()}/{hash}.jpg";
    }

    public static string? HeaderImageUrl(string? appId)
    {
        if (string.IsNullOrWhiteSpace(appId))
        {
            return null;
        }

        return $"{HeaderCdn}/{appId.Trim()}/header.jpg";
    }

    public static string RequireTrimmedKey(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{fieldName} is required.", fieldName);
        }

        return value.Trim();
    }
}
