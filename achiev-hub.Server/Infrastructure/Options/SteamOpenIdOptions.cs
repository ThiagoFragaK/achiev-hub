namespace achiev_hub.Server.Infrastructure.Options;

public class SteamOpenIdOptions
{
    public const string SectionName = "SteamOpenId";

    /// <summary>Exact return_to URL registered for the OpenID transaction (no query extras beyond what we send).</summary>
    public string ReturnTo { get; set; } = "https://localhost:7157/api/auth/steam/callback";

    public string Realm { get; set; } = "https://localhost:7157/";

    public string OpEndpoint { get; set; } = "https://steamcommunity.com/openid/login";

    public string ClaimedIdPrefix { get; set; } = "https://steamcommunity.com/openid/id/";

    /// <summary>SPA path after successful login.</summary>
    public string FrontendSuccessPath { get; set; } = "/";

    /// <summary>SPA path after failed login.</summary>
    public string FrontendErrorPath { get; set; } = "/login";
}
