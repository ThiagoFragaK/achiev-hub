namespace achiev_hub.Server.Infrastructure.Options;

public class SteamOpenIdOptions
{
    public const string SectionName = "SteamOpenId";
    public string PublicOrigin { get; set; } = "";
    public string ReturnTo { get; set; } = "";
    public string Realm { get; set; } = "";
    public string OpEndpoint { get; set; } = "https://steamcommunity.com/openid/login";
    public string ClaimedIdPrefix { get; set; } = "https://steamcommunity.com/openid/id/";
    public string FrontendSuccessPath { get; set; } = "/";
    public string FrontendErrorPath { get; set; } = "/login";
}
