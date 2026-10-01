using achiev_hub.Server.Application.Auth;
using achiev_hub.Server.Application.Auth.Interfaces;
using achiev_hub.Server.Application.Steam.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace achiev_hub.Server.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/register")]
public class RegistrationController : ControllerBase
{
    private readonly IRegistrationService _service;
    private readonly ISyncStatusService _syncStatusService;

    public RegistrationController(IRegistrationService service, ISyncStatusService syncStatusService)
    {
        _service = service;
        _syncStatusService = syncStatusService;
    }

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

    [HttpGet("status")]
    public async Task<IActionResult> GetProvisioningStatus(
        [FromQuery] string steamId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(steamId))
        {
            return UnprocessableEntity(new { message = "steamId is required" });
        }

        var status = await _syncStatusService.GetProvisioningStatusBySteamIdAsync(steamId, cancellationToken);
        if (status is null)
        {
            return NotFound(new { message = "User not found" });
        }

        return Ok(status);
    }

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
