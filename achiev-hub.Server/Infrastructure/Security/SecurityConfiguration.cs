using System.Text;
using achiev_hub.Server.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace achiev_hub.Server.Infrastructure.Security;

public static class SecurityConfiguration
{
    public const int MinimumJwtKeyBytes = 32;

    /// <summary>
    /// Validates that Steam API key and JWT signing key are present and adequately strong.
    /// Development may use a local placeholder JWT key; Steam API key is always required.
    /// </summary>
    public static void Validate(IConfiguration configuration, IHostEnvironment environment)
    {
        var steamKey = configuration.GetSection(SteamApiOptions.SectionName).GetValue<string>("ApiKey");
        if (string.IsNullOrWhiteSpace(steamKey))
        {
            throw new InvalidOperationException(
                "SteamApi:ApiKey is missing. Set user secrets or environment variable SteamApi__ApiKey / STEAM_API_KEY.");
        }

        var jwtKey = configuration.GetSection("Jwt").GetValue<string>("Key");
        if (string.IsNullOrWhiteSpace(jwtKey))
        {
            throw new InvalidOperationException(
                "Jwt:Key is missing. Set user secrets or environment variable Jwt__Key.");
        }

        var keyBytes = Encoding.UTF8.GetByteCount(jwtKey);
        if (keyBytes < MinimumJwtKeyBytes)
        {
            throw new InvalidOperationException(
                $"Jwt:Key must be at least {MinimumJwtKeyBytes} bytes (UTF-8); got {keyBytes}.");
        }

        if (!environment.IsDevelopment())
        {
            if (jwtKey.Contains("ChangeInProduction", StringComparison.OrdinalIgnoreCase)
                || jwtKey.Contains("DevSecret", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Jwt:Key must not use a development placeholder outside Development.");
            }

            var openId = configuration.GetSection(SteamOpenIdOptions.SectionName);
            var returnTo = openId.GetValue<string>("ReturnTo");
            var realm = openId.GetValue<string>("Realm");
            var publicOrigin = openId.GetValue<string>("PublicOrigin");
            if (ContainsLocalhost(returnTo) || ContainsLocalhost(realm) || ContainsLocalhost(publicOrigin))
            {
                throw new InvalidOperationException(
                    "SteamOpenId ReturnTo/Realm/PublicOrigin must not use localhost outside Development. " +
                    "Set SteamOpenId__PublicOrigin to your public HTTPS origin (e.g. https://your-app.example), " +
                    "or leave them empty to derive from the incoming request host.");
            }
        }
    }

    private static bool ContainsLocalhost(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && value.Contains("localhost", StringComparison.OrdinalIgnoreCase);
}
