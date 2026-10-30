using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using achiev_hub.Server.Infrastructure.Auth;

namespace achiev_hub.Server.Infrastructure.Security;

public static class AccessControl
{
    public const string AdminRole = "admin";

    public static bool IsAdmin(ClaimsPrincipal user) =>
        user.IsInRole(AdminRole);

    public static bool IsGuest(ClaimsPrincipal user) =>
        user.IsInRole(JwtTokenService.GuestRole)
        || user.FindFirstValue(JwtTokenService.TokenKindClaim) == JwtTokenService.GuestTokenKind;

    public static int? TryGetUserId(ClaimsPrincipal user)
    {
        if (IsGuest(user))
        {
            return null;
        }

        var raw = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return int.TryParse(raw, out var id) && id > 0 ? id : null;
    }

    /// <summary>True when the principal may read/mutate a resource owned by <paramref name="resourceUserId"/>.</summary>
    public static bool CanAccessOwnedResource(ClaimsPrincipal user, int resourceUserId)
    {
        if (IsAdmin(user))
        {
            return true;
        }

        var userId = TryGetUserId(user);
        return userId is int id && id == resourceUserId;
    }

    public static string? TryGetSteamId(ClaimsPrincipal user) =>
        user.FindFirstValue("steam_id");
}
