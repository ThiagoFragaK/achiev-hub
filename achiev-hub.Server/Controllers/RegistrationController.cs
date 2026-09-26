using achiev_hub.Server.DTOs.Auth;
using achiev_hub.Server.Services;
using achiev_hub.Server.Services.Interfaces;
using achiev_hub.Server.Support;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace achiev_hub.Server.Controllers;

[ApiController]
[AllowAnonymous]
[EnableRateLimiting("auth")]
[Route("api/register")]
public class RegistrationController : ControllerBase
{
    private readonly IRegistrationService _service;

    public RegistrationController(IRegistrationService service)
    {
        _service = service;
    }

    [HttpPost("send-verification")]
    public async Task<IActionResult> SendVerification(
        [FromBody] SendVerificationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.SendVerificationAsync(request.Email, cancellationToken);
            if (AuthResult.IsError(result, out var error))
            {
                return StatusCode(error.HttpStatus, new { message = error.Message });
            }

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(503, new { message = ex.Message });
        }
    }

    [HttpPost("confirm-code")]
    public async Task<IActionResult> ConfirmCode(
        [FromBody] ConfirmCodeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _service.ConfirmCodeAsync(request.Email, request.Code, cancellationToken);
        if (AuthResult.IsError(result, out var error))
        {
            return StatusCode(error.HttpStatus, new { message = error.Message });
        }

        return Ok(new
        {
            success = true,
            message = "Email verified",
            data = result
        });
    }

    [HttpPost]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        if (!SteamIdValidator.IsValidSteamId64(request.SteamId))
        {
            return UnprocessableEntity(new { message = "Steam ID must be a 17-digit SteamID64" });
        }

        var result = await _service.RegisterAsync(request, cancellationToken);
        if (AuthResult.IsError(result, out var error))
        {
            return StatusCode(error.HttpStatus, new { message = error.Message });
        }

        return StatusCode(StatusCodes.Status201Created, new
        {
            success = true,
            message = "Registration successful",
            data = result
        });
    }
}
