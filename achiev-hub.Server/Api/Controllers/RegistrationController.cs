using achiev_hub.Server.Application.Auth;
using achiev_hub.Server.Application.Auth.Interfaces;
using achiev_hub.Server.Application.Steam.Interfaces;
using achiev_hub.Server.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace achiev_hub.Server.Api.Controllers;

[ApiController]
[Route("api/register")]
[EnableRateLimiting("auth")]
public class RegistrationController : ControllerBase
{
    private readonly IRegistrationService _service;
    private readonly ISyncStatusService _syncStatusService;

    public RegistrationController(IRegistrationService service, ISyncStatusService syncStatusService)
    {
        _service = service;
        _syncStatusService = syncStatusService;
    }

    [AllowAnonymous]
    [HttpPost("validate-steam")]
    public async Task<IActionResult> ValidateSteam(
        [FromBody] ValidateSteamRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _service.ValidateSteamAsync(request.SteamId, cancellationToken);
        if (AuthResult.IsError(result, out var error))
        {
            return StatusCode(error.HttpStatus, new { message = error.Message });
        }

        return Ok(result);
    }

    [Authorize]
    [HttpGet("status")]
    public async Task<IActionResult> GetProvisioningStatus(
        [FromQuery] string? steamId,
        CancellationToken cancellationToken)
    {
        if (AccessControl.IsGuest(User))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Guests cannot query provisioning status." });
        }

        var claimSteamId = AccessControl.TryGetSteamId(User);
        if (!SteamIdBinder.TryResolve(claimSteamId, steamId, out var boundSteamId, out var bindError))
        {
            return BadRequest(new { message = bindError });
        }

        var status = await _syncStatusService.GetProvisioningStatusBySteamIdAsync(boundSteamId, cancellationToken);
        if (status is null)
        {
            // Uniform response — do not reveal whether another SteamId exists.
            return Ok(new { status = "unknown", message = "No provisioning data for this account." });
        }

        return Ok(status);
    }

    [AllowAnonymous]
    [HttpPost("send-verification")]
    public async Task<IActionResult> SendVerification(
        [FromBody] SendVerificationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.SendVerificationAsync(request.Email, request.SteamId, cancellationToken);
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

    [AllowAnonymous]
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

    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _service.RegisterAsync(request, cancellationToken);
        if (AuthResult.IsError(result, out var error))
        {
            return StatusCode(error.HttpStatus, new { message = error.Message });
        }

        return StatusCode(StatusCodes.Status201Created, new
        {
            success = true,
            message = "Registration successful. Importing your recently played games…",
            data = result
        });
    }
}
