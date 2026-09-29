using achiev_hub.Server.Models;

namespace achiev_hub.Server.Support;

public static class SteamVisibility
{
    /// <summary>Steam Web API: 3 = public profile.</summary>
    public const int PublicCommunityVisibility = 3;

    public static bool IsLibraryPublic(Player? player) =>
        player?.CommunityVisibilityState == PublicCommunityVisibility;

    public static bool IsLibraryPublic(int? communityVisibilityState) =>
        communityVisibilityState == PublicCommunityVisibility;
}
