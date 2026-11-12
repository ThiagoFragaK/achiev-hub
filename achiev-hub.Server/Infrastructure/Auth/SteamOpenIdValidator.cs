using System.Text.RegularExpressions;
using achiev_hub.Server.Infrastructure.Options;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace achiev_hub.Server.Infrastructure.Auth;

public sealed class SteamOpenIdValidator
{
    public const string StateCookieName = "achiev_hub_oid_state";
    private const string CallbackPath = "/api/auth/steam/callback";
    private static readonly Regex SteamId64Regex = new(@"^7656119\d{10}$", RegexOptions.Compiled);

    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly SteamOpenIdOptions _options;

    public SteamOpenIdValidator(
        HttpClient httpClient,
        IMemoryCache cache,
        IOptions<SteamOpenIdOptions> options)
    {
        _httpClient = httpClient;
        _cache = cache;
        _options = options.Value;
    }

    public string CreateState()
    {
        return Convert.ToBase64String(Guid.NewGuid().ToByteArray())
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    /// <summary>Builds return_to for tests or when ReturnTo is configured explicitly.</summary>
    public string BuildReturnTo(string state) =>
        QueryHelpers.AddQueryString(ResolveReturnToBase(request: null), "state", state);

    public Uri BuildLoginRedirectUri(string state, HttpRequest request)
    {
        var returnToBase = ResolveReturnToBase(request);
        var realm = ResolveRealm(request);
        var returnTo = QueryHelpers.AddQueryString(returnToBase, "state", state);

        _cache.Set(StateCacheKey(state), returnTo, TimeSpan.FromMinutes(10));

        var query = new Dictionary<string, string?>
        {
            ["openid.ns"] = "http://specs.openid.net/auth/2.0",
            ["openid.mode"] = "checkid_setup",
            ["openid.return_to"] = returnTo,
            ["openid.realm"] = realm,
            ["openid.identity"] = "http://specs.openid.net/auth/2.0/identifier_select",
            ["openid.claimed_id"] = "http://specs.openid.net/auth/2.0/identifier_select"
        };

        return new Uri(QueryHelpers.AddQueryString(_options.OpEndpoint, query));
    }

    public async Task<SteamOpenIdResult> ValidateCallbackAsync(
        IQueryCollection query,
        string? stateFromCookie,
        CancellationToken cancellationToken = default)
    {
        if (!string.Equals(query["openid.mode"], "id_res", StringComparison.Ordinal))
        {
            return SteamOpenIdResult.Fail("Invalid openid.mode.");
        }

        if (!string.Equals(query["openid.op_endpoint"], _options.OpEndpoint, StringComparison.Ordinal))
        {
            return SteamOpenIdResult.Fail("Invalid openid.op_endpoint.");
        }

        if (string.IsNullOrWhiteSpace(stateFromCookie)
            || !string.Equals(query["state"], stateFromCookie, StringComparison.Ordinal)
            || !TryConsumeExpectedReturnTo(stateFromCookie, out var expectedReturnTo))
        {
            return SteamOpenIdResult.Fail("Invalid or reused OpenID state.");
        }

        var returnTo = query["openid.return_to"].ToString();
        if (!string.Equals(returnTo, expectedReturnTo, StringComparison.Ordinal))
        {
            return SteamOpenIdResult.Fail("Invalid openid.return_to.");
        }

        var claimedId = query["openid.claimed_id"].ToString();
        if (!claimedId.StartsWith(_options.ClaimedIdPrefix, StringComparison.Ordinal))
        {
            return SteamOpenIdResult.Fail("Invalid openid.claimed_id prefix.");
        }

        var steamId = claimedId[_options.ClaimedIdPrefix.Length..];
        if (!SteamId64Regex.IsMatch(steamId))
        {
            return SteamOpenIdResult.Fail("Invalid SteamID64.");
        }

        var identity = query["openid.identity"].ToString();
        if (!string.Equals(identity, claimedId, StringComparison.Ordinal))
        {
            return SteamOpenIdResult.Fail("openid.identity must match claimed_id.");
        }

        var responseNonce = query["openid.response_nonce"].ToString();
        if (string.IsNullOrWhiteSpace(responseNonce))
        {
            return SteamOpenIdResult.Fail("Missing openid.response_nonce.");
        }

        var nonceKey = $"openid:nonce:{responseNonce}";
        if (_cache.TryGetValue(nonceKey, out _))
        {
            return SteamOpenIdResult.Fail("Replayed OpenID response.");
        }

        if (!await CheckAuthenticationAsync(query, cancellationToken))
        {
            return SteamOpenIdResult.Fail("Steam check_authentication failed.");
        }

        _cache.Set(nonceKey, true, TimeSpan.FromHours(24));
        return SteamOpenIdResult.Ok(steamId);
    }

    internal string ResolveReturnToBase(HttpRequest? request)
    {
        if (!string.IsNullOrWhiteSpace(_options.ReturnTo))
        {
            return _options.ReturnTo.Trim();
        }

        return $"{ResolveOrigin(request)}{CallbackPath}";
    }

    internal string ResolveRealm(HttpRequest? request)
    {
        if (!string.IsNullOrWhiteSpace(_options.Realm))
        {
            var realm = _options.Realm.Trim();
            return realm.EndsWith('/') ? realm : realm + "/";
        }

        return ResolveOrigin(request) + "/";
    }

    private string ResolveOrigin(HttpRequest? request)
    {
        if (!string.IsNullOrWhiteSpace(_options.PublicOrigin))
        {
            return _options.PublicOrigin.Trim().TrimEnd('/');
        }

        if (request is null)
        {
            throw new InvalidOperationException(
                "SteamOpenId:ReturnTo or SteamOpenId:PublicOrigin must be configured when no HTTP request is available.");
        }

        return $"{request.Scheme}://{request.Host.Value}".TrimEnd('/');
    }

    private bool TryConsumeExpectedReturnTo(string state, out string expectedReturnTo)
    {
        expectedReturnTo = "";
        var key = StateCacheKey(state);
        if (!_cache.TryGetValue(key, out string? stored) || string.IsNullOrEmpty(stored))
        {
            return false;
        }

        _cache.Remove(key);
        expectedReturnTo = stored;
        return true;
    }

    private async Task<bool> CheckAuthenticationAsync(IQueryCollection query, CancellationToken cancellationToken)
    {
        var form = new Dictionary<string, string>();
        foreach (var key in query.Keys)
        {
            if (key.StartsWith("openid.", StringComparison.Ordinal))
            {
                form[key] = query[key].ToString();
            }
        }

        form["openid.mode"] = "check_authentication";

        using var content = new FormUrlEncodedContent(form);
        using var response = await _httpClient.PostAsync(_options.OpEndpoint, content, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        return body.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(line => string.Equals(line, "is_valid:true", StringComparison.OrdinalIgnoreCase));
    }

    private static string StateCacheKey(string state) => $"openid:state:{state}";
}

public sealed record SteamOpenIdResult(bool Success, string? SteamId, string? Error)
{
    public static SteamOpenIdResult Ok(string steamId) => new(true, steamId, null);
    public static SteamOpenIdResult Fail(string error) => new(false, null, error);
}
