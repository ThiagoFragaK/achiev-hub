using System.Security.Claims;
using System.Text.Json;
using achiev_hub.Server.Application.Auth;
using achiev_hub.Server.Application.Auth.Interfaces;
using achiev_hub.Server.Infrastructure.Auth;
using achiev_hub.Server.Infrastructure.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace achiev_hub.Server.Api.Controllers;

[ApiController]
[Route("api/auth/steam")]
[EnableRateLimiting("auth")]
public class SteamOpenIdController : ControllerBase
{
    private readonly SteamOpenIdValidator _openId;
    private readonly IAuthenticationService _auth;
    private readonly IMemoryCache _cache;
    private readonly SteamOpenIdOptions _options;
    private readonly JwtSettings _jwtSettings;

    public SteamOpenIdController(
        SteamOpenIdValidator openId,
        IAuthenticationService auth,
        IMemoryCache cache,
        IOptions<SteamOpenIdOptions> options,
        IOptions<JwtSettings> jwtSettings)
    {
        _openId = openId;
        _auth = auth;
        _cache = cache;
        _options = options.Value;
        _jwtSettings = jwtSettings.Value;
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Begin([FromQuery] bool link = false)
    {
        var state = _openId.CreateState();
        if (link)
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return Unauthorized(new { message = "Login required to link a Steam account." });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userId, out var id))
            {
                return Unauthorized();
            }

            _cache.Set(LinkCacheKey(state), id, TimeSpan.FromMinutes(10));
        }

        Response.Cookies.Append(
            SteamOpenIdValidator.StateCookieName,
            state,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                IsEssential = true,
                MaxAge = TimeSpan.FromMinutes(10),
                Path = "/"
            });

        return Redirect(_openId.BuildLoginRedirectUri(state).ToString());
    }

    [AllowAnonymous]
    [HttpGet("callback")]
    public async Task<IActionResult> Callback(CancellationToken cancellationToken)
    {
        Request.Cookies.TryGetValue(SteamOpenIdValidator.StateCookieName, out var stateCookie);
        Response.Cookies.Delete(SteamOpenIdValidator.StateCookieName);

        var validation = await _openId.ValidateCallbackAsync(Request.Query, stateCookie, cancellationToken);
        if (!validation.Success)
        {
            return Redirect($"{_options.FrontendErrorPath}?steam_error={Uri.EscapeDataString(validation.Error ?? "openid_failed")}");
        }

        object authResult;
        if (_cache.TryGetValue(LinkCacheKey(stateCookie!), out int linkUserId))
        {
            _cache.Remove(LinkCacheKey(stateCookie!));
            authResult = await _auth.LinkVerifiedSteamIdAsync(linkUserId, validation.SteamId!, cancellationToken);
        }
        else
        {
            authResult = await _auth.LoginWithVerifiedSteamIdAsync(validation.SteamId!, cancellationToken);
        }

        if (AuthResult.IsError(authResult, out var error))
        {
            var q = error.HttpStatus == 404
                ? $"steamId={Uri.EscapeDataString(validation.SteamId!)}&steam_error={Uri.EscapeDataString(error.Message)}"
                : $"steam_error={Uri.EscapeDataString(error.Message)}";
            return Redirect($"{_options.FrontendErrorPath}?{q}");
        }

        var login = (LoginResponseDto)authResult;
        var code = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        _cache.Set(
            ExchangeCacheKey(code),
            JsonSerializer.Serialize(login),
            TimeSpan.FromMinutes(2));

        return Redirect($"/login?steam_code={Uri.EscapeDataString(code)}");
    }

    [AllowAnonymous]
    [HttpPost("exchange")]
    public IActionResult Exchange([FromBody] SteamCodeExchangeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
        {
            return BadRequest(new { message = "code is required" });
        }

        var key = ExchangeCacheKey(request.Code.Trim());
        if (!_cache.TryGetValue(key, out string? json) || string.IsNullOrEmpty(json))
        {
            return Unauthorized(new { message = "Invalid or expired Steam login code." });
        }

        _cache.Remove(key);
        var login = JsonSerializer.Deserialize<LoginResponseDto>(json);
        if (login is null || string.IsNullOrWhiteSpace(login.AccessToken))
        {
            return Unauthorized(new { message = "Invalid or expired Steam login code." });
        }

        AuthCookie.Append(Response, Request, login.AccessToken, _jwtSettings.ExpiryMinutes);
        return Ok(new
        {
            success = true,
            data = new
            {
                accessToken = (string?)null,
                tokenType = "Cookie",
                user = login.User
            }
        });
    }

    private static string LinkCacheKey(string state) => $"openid:link:{state}";
    private static string ExchangeCacheKey(string code) => $"openid:exchange:{code}";
}

public sealed class SteamCodeExchangeRequest
{
    public string Code { get; set; } = string.Empty;
}
