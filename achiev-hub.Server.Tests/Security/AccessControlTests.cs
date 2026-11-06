using System.Security.Claims;
using achiev_hub.Server.Infrastructure.Security;

namespace achiev_hub.Server.Tests.Security;

public class AccessControlTests
{
    private static ClaimsPrincipal Principal(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, authenticationType: "Test"));

    [Fact]
    public void CanAccessOwnedResource_denies_other_users()
    {
        var user = Principal(
            new Claim(ClaimTypes.NameIdentifier, "10"),
            new Claim(ClaimTypes.Role, "user"));

        Assert.False(AccessControl.CanAccessOwnedResource(user, resourceUserId: 99));
    }

    [Fact]
    public void CanAccessOwnedResource_allows_owner()
    {
        var user = Principal(
            new Claim(ClaimTypes.NameIdentifier, "10"),
            new Claim(ClaimTypes.Role, "user"));

        Assert.True(AccessControl.CanAccessOwnedResource(user, resourceUserId: 10));
    }

    [Fact]
    public void CanAccessOwnedResource_allows_admin_for_any_user()
    {
        var user = Principal(
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Role, AccessControl.AdminRole));

        Assert.True(AccessControl.CanAccessOwnedResource(user, resourceUserId: 99));
    }
}
