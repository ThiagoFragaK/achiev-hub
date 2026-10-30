using achiev_hub.Server.Infrastructure.Auth;
using achiev_hub.Server.Infrastructure.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace achiev_hub.Server.Tests.Security;

public class SteamOpenIdValidatorTests
{
    private static SteamOpenIdValidator Create(
        HttpMessageHandler handler,
        out IMemoryCache cache,
        SteamOpenIdOptions? options = null)
    {
        cache = new MemoryCache(new MemoryCacheOptions());
        var opts = MsOptions.Create(options ?? new SteamOpenIdOptions
        {
            ReturnTo = "https://app.example/api/auth/steam/callback",
            Realm = "https://app.example/",
            OpEndpoint = "https://steamcommunity.com/openid/login",
            ClaimedIdPrefix = "https://steamcommunity.com/openid/id/"
        });
        return new SteamOpenIdValidator(new HttpClient(handler), cache, opts);
    }

    [Fact]
    public async Task ValidateCallback_rejects_forged_params_without_check_authentication()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage
        {
            Content = new StringContent("ns:http://specs.openid.net/auth/2.0\nis_valid:false\n")
        });
        var validator = Create(handler, out _);
        var state = validator.CreateState();
        var returnTo = validator.BuildReturnTo(state);

        var query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
        {
            ["openid.mode"] = "id_res",
            ["openid.op_endpoint"] = "https://steamcommunity.com/openid/login",
            ["openid.return_to"] = returnTo,
            ["openid.claimed_id"] = "https://steamcommunity.com/openid/id/76561198000000001",
            ["openid.identity"] = "https://steamcommunity.com/openid/id/76561198000000001",
            ["openid.response_nonce"] = "2026-01-01T00:00:00Zunique1",
            ["state"] = state
        });

        var result = await validator.ValidateCallbackAsync(query, state);
        Assert.False(result.Success);
        Assert.Contains("check_authentication", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ValidateCallback_accepts_valid_steam_response_once_only()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage
        {
            Content = new StringContent("ns:http://specs.openid.net/auth/2.0\nis_valid:true\n")
        });
        var validator = Create(handler, out _);
        var state = validator.CreateState();
        var returnTo = validator.BuildReturnTo(state);
        const string nonce = "2026-01-01T00:00:00Zunique2";

        var query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
        {
            ["openid.mode"] = "id_res",
            ["openid.op_endpoint"] = "https://steamcommunity.com/openid/login",
            ["openid.return_to"] = returnTo,
            ["openid.claimed_id"] = "https://steamcommunity.com/openid/id/76561198000000001",
            ["openid.identity"] = "https://steamcommunity.com/openid/id/76561198000000001",
            ["openid.response_nonce"] = nonce,
            ["state"] = state
        });

        var first = await validator.ValidateCallbackAsync(query, state);
        Assert.True(first.Success);
        Assert.Equal("76561198000000001", first.SteamId);

        // State already consumed — replay with same state fails even before nonce.
        var state2 = validator.CreateState();
        var returnTo2 = validator.BuildReturnTo(state2);
        var queryReplay = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
        {
            ["openid.mode"] = "id_res",
            ["openid.op_endpoint"] = "https://steamcommunity.com/openid/login",
            ["openid.return_to"] = returnTo2,
            ["openid.claimed_id"] = "https://steamcommunity.com/openid/id/76561198000000001",
            ["openid.identity"] = "https://steamcommunity.com/openid/id/76561198000000001",
            ["openid.response_nonce"] = nonce,
            ["state"] = state2
        });

        var second = await validator.ValidateCallbackAsync(queryReplay, state2);
        Assert.False(second.Success);
        Assert.Contains("Replay", second.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ValidateCallback_rejects_wrong_op_endpoint()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage
        {
            Content = new StringContent("is_valid:true\n")
        });
        var validator = Create(handler, out _);
        var state = validator.CreateState();

        var query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
        {
            ["openid.mode"] = "id_res",
            ["openid.op_endpoint"] = "https://evil.example/openid",
            ["openid.return_to"] = validator.BuildReturnTo(state),
            ["openid.claimed_id"] = "https://steamcommunity.com/openid/id/76561198000000001",
            ["openid.identity"] = "https://steamcommunity.com/openid/id/76561198000000001",
            ["openid.response_nonce"] = "n",
            ["state"] = state
        });

        var result = await validator.ValidateCallbackAsync(query, state);
        Assert.False(result.Success);
        Assert.Contains("op_endpoint", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) => _responder = responder;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(_responder(request));
    }
}
