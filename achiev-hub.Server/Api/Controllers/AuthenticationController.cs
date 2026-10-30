using System.Security.Claims;
using achiev_hub.Server.Application.Auth;
using achiev_hub.Server.Application.Auth.Interfaces;
using achiev_hub.Server.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace achiev_hub.Server.Api.Controllers;

[ApiController]
[Route("api")]
public class AuthenticationController : ControllerBase
{
    private readonly IAuthenticationService _service;
    private readonly JwtSettings _jwtSettings;

    public AuthenticationController(IAuthenticationService service, IOptions<JwtSettings> jwtSettings)
    {
        _service = service;
        _jwtSettings = jwtSettings.Value;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.SteamId) || string.IsNullOrWhiteSpace(request.Password))
        {
            return UnprocessableEntity(new { message = "Steam ID and password are required" });
        }

        var response = await _service.LoginAsync(request.SteamId, request.Password, cancellationToken);
        if (AuthResult.IsError(response, out var error))
        {
            return StatusCode(error.HttpStatus, new { message = error.Message });
        }

        return Ok(IssueSession((LoginResponseDto)response));
    }

    [AllowAnonymous]
    [HttpPost("guest")]
    public IActionResult ContinueAsGuest([FromBody] GuestRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.SteamId))
        {
            return UnprocessableEntity(new { message = "Steam ID is required" });
        }

        var response = _service.ContinueAsGuest(request.SteamId);
        return Ok(IssueSession((LoginResponseDto)response));
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var isGuest = User.IsInRole(JwtTokenService.GuestRole)
            || User.FindFirstValue(JwtTokenService.TokenKindClaim) == JwtTokenService.GuestTokenKind;
        if (!isGuest)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Invalid token" });
            }

            await _service.LogoutAsync(userId, cancellationToken);
        }

        AuthCookie.Delete(Response, Request);
        return Ok(new { success = true, message = "Logged out successfully" });
    }

    private object IssueSession(LoginResponseDto login)
    {
        AuthCookie.Append(Response, Request, login.AccessToken, _jwtSettings.ExpiryMinutes);
        // Do not return the raw JWT to JavaScript — cookie is the session credential.
        return new
        {
            success = true,
            message = "Login successful",
            data = new
            {
                accessToken = (string?)null,
                tokenType = "Cookie",
                user = login.User
            }
        };
    }
}
