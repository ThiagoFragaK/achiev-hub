using achiev_hub.Server.Infrastructure.Security;

namespace achiev_hub.Server.Tests.Security;

public class SteamIdBinderTests
{
    [Fact]
    public void TryResolve_rejects_mismatched_query_steamId()
    {
        var ok = SteamIdBinder.TryResolve(
            tokenSteamId: "76561198000000001",
            requestedSteamId: "76561198000000099",
            out _,
            out var error);

        Assert.False(ok);
        Assert.Contains("does not match", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TryResolve_allows_matching_or_omitted_query()
    {
        Assert.True(SteamIdBinder.TryResolve("76561198000000001", null, out var a, out _));
        Assert.Equal("76561198000000001", a);

        Assert.True(SteamIdBinder.TryResolve("76561198000000001", "76561198000000001", out var b, out _));
        Assert.Equal("76561198000000001", b);
    }

    [Fact]
    public void TryResolve_requires_token_claim()
    {
        Assert.False(SteamIdBinder.TryResolve(null, "76561198000000001", out _, out var error));
        Assert.Contains("missing", error, StringComparison.OrdinalIgnoreCase);
    }
}
