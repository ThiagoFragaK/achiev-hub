using achiev_hub.Server.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SteamSync.Shared;

namespace achiev_hub.Server.Controllers;

[Authorize]
[EnableRateLimiting("steam")]
[ApiController]
[Route("api/users")]
public class UsersController : ApiControllerBase
{
    private readonly ISyncStatusService _syncStatusService;

    public UsersController(ISyncStatusService syncStatusService)
    {
        _syncStatusService = syncStatusService;
    }

    /// <summary>Returns sync progress for the given user (must be the authenticated user).</summary>
    [HttpGet("{id:int}/sync-status")]
    public async Task<ActionResult<UserSyncStatusDto>> GetSyncStatus(int id, CancellationToken cancellationToken)
    {
        if (IsGuest())
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Guests have no sync status." });
        }

        if (ResolveUserId() is not int userId)
        {
            return Unauthorized(new { message = "Invalid token" });
        }

        if (userId != id)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Cannot view another user's sync status." });
        }

        var status = await _syncStatusService.GetUserSyncStatusAsync(id, cancellationToken);
        return status is null ? NotFound() : Ok(status);
    }
}
