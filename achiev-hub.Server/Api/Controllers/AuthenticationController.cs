using System.Security.Claims;
using achiev_hub.Server.Application.Auth;
using achiev_hub.Server.Application.Auth.Interfaces;
using achiev_hub.Server.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace achiev_hub.Server.Api.Controllers;

[ApiController]
[Route("api")]
[EnableRateLimiting("auth")]
public class AuthenticationController : ControllerBase
{
    private readonly IAuthenticationService _authenticationService;
    private readonly JwtSettings _jwtSettings;

    public AuthenticationController(IAuthenticationService AuthenticationService, IOptions<JwtSettings> jwtSettings)
    {
        _authenticationService = AuthenticationService;
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

        var response = await _authenticationService.LoginAsync(request.SteamId, request.Password, cancellationToken);
        if (AuthResult.IsError(response, out var error))
        {
            return StatusCode(error.HttpStatus, new { message = error.Message });
        }

        return Ok(IssueSession((LoginResponseDto)response));
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "Invalid token" });
        }

        await _authenticationService.LogoutAsync(userId, cancellationToken);
        AuthCookie.Delete(Response, Request);
        return Ok(new { success = true, message = "Logged out successfully" });
    }

    private object IssueSession(LoginResponseDto login)
    {
        AuthCookie.Append(Response, Request, login.AccessToken, _jwtSettings.ExpiryMinutes);
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
