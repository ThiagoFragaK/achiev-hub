using achiev_hub.Server.Application.Steam;
using achiev_hub.Server.Application.Steam.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace achiev_hub.Server.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/steam/sync")]
public class SteamSyncController : ApiControllerBase
{
    private readonly ISyncStatusService _syncStatusService;

    public SteamSyncController(ISyncStatusService syncStatusService)
    {
        _syncStatusService = syncStatusService;
    }

    [HttpGet("status")]
    public async Task<ActionResult<SyncStatusDto>> GetStatus(CancellationToken cancellationToken)
    {
        if (IsGuest())
        {
            return Unauthorized(new { message = "Sign in to view sync status." });
        }

        var userId = ResolveUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var status = await _syncStatusService.GetStatusForUserAsync(userId.Value, cancellationToken);
        if (status is null)
        {
            return NotFound(new { message = "User not found" });
        }

        return Ok(status);
    }
}
