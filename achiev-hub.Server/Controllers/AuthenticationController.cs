using System.Security.Claims;
using achiev_hub.Server.DTOs.Auth;
using achiev_hub.Server.Services;
using achiev_hub.Server.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

        return Ok(new
        {
            success = true,
            message = "Login successful",
            data = response
        });
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

        await _service.LogoutAsync(userId, cancellationToken);
        return Ok(new { success = true, message = "Logged out successfully" });
    }
}
