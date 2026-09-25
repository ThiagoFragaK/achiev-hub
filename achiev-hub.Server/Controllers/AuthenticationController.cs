using System.Security.Claims;
using achiev_hub.Server.DTOs.Auth;
using achiev_hub.Server.Services;
using achiev_hub.Server.Services.Interfaces;
using achiev_hub.Server.Support;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace achiev_hub.Server.Controllers;

[ApiController]
[Route("api")]
public class AuthenticationController : ControllerBase
{
    private readonly IAuthenticationService _service;

    public AuthenticationController(IAuthenticationService service)
    {
        _service = service;
    }

    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.SteamId) || string.IsNullOrWhiteSpace(request.Password))
        {
            return UnprocessableEntity(new { message = "Steam ID and password are required" });
        }

        if (!SteamIdValidator.IsValidSteamId64(request.SteamId))
        {
            return UnprocessableEntity(new { message = "Steam ID must be a 17-digit SteamID64" });
        }

        var response = await _service.LoginAsync(request.SteamId, request.Password, cancellationToken);
        if (AuthResult.IsError(response, out var error))
        {
            return StatusCode(error.HttpStatus, new { message = error.Message });
        }

        return Ok(new
        {
            success = true,
            message = "Login successful",
            data = response
        });
    }

    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [HttpPost("guest")]
    public IActionResult ContinueAsGuest([FromBody] GuestRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.SteamId))
        {
            return UnprocessableEntity(new { message = "Steam ID is required" });
        }

        if (!SteamIdValidator.IsValidSteamId64(request.SteamId))
        {
            return UnprocessableEntity(new { message = "Steam ID must be a 17-digit SteamID64" });
        }

        var response = _service.ContinueAsGuest(request.SteamId);
        return Ok(new
        {
            success = true,
            message = "Guest session created",
            data = response
        });
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var isGuest = User.IsInRole(JwtTokenService.GuestRole)
            || User.FindFirstValue(JwtTokenService.TokenKindClaim) == JwtTokenService.GuestTokenKind;
        if (isGuest)
        {
            return Ok(new { success = true, message = "Logged out successfully" });
        }

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "Invalid token" });
        }

        await _service.LogoutAsync(userId, cancellationToken);
        return Ok(new { success = true, message = "Logged out successfully" });
    }
}
