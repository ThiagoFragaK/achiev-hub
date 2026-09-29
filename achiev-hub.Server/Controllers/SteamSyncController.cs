using achiev_hub.Server.DTOs;
using achiev_hub.Server.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace achiev_hub.Server.Controllers;

[Authorize]
[EnableRateLimiting("steam")]
[ApiController]
[Route("api/steam/sync")]
public class SteamSyncController : ApiControllerBase
{
    private readonly ISyncStatusService _syncStatusService;
    private readonly ISyncJobEnqueueService _enqueueService;
    private readonly ISteamVisibilityService _steamVisibilityService;

    public SteamSyncController(
        ISyncStatusService syncStatusService,
        ISyncJobEnqueueService enqueueService,
        ISteamVisibilityService steamVisibilityService)
    {
        _syncStatusService = syncStatusService;
        _enqueueService = enqueueService;
        _steamVisibilityService = steamVisibilityService;
    }

    [HttpGet("status")]
    public async Task<ActionResult<SyncStatusDto>> GetStatus(CancellationToken cancellationToken)
    {
        if (IsGuest())
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Guests have no sync status." });
        }

        if (ResolveUserId() is not int userId)
        {
            return Unauthorized(new { message = "Invalid token" });
        }

        var status = await _syncStatusService.GetStatusForUserAsync(userId, cancellationToken);
        return status is null ? NotFound() : Ok(status);
    }

    [HttpPost("games/{appId:int}")]
    public async Task<ActionResult<EnqueueSyncResponseDto>> EnqueueGameAchievements(
        int appId,
        CancellationToken cancellationToken)
    {
        if (IsGuest())
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Guests cannot sync." });
        }

        if (ResolveUserId() is not int userId)
        {
            return Unauthorized(new { message = "Invalid token" });
        }

        if (RequireSteamId(out var steamId) is { } error)
        {
            return error;
        }

        var isPublic = await _steamVisibilityService.RefreshUserSteamVisibilityAsync(userId, steamId, cancellationToken);
        if (!isPublic)
        {
            return Conflict(new
            {
                message = "Your Steam profile is private. Make game details public, then try syncing again."
            });
        }

        var jobId = await _enqueueService.EnqueueGameAchievementSyncAsync(
            userId,
            steamId,
            appId,
            cancellationToken);

        return Accepted(new EnqueueSyncResponseDto
        {
            Message = "Achievement sync enqueued.",
            JobIds = [jobId]
        });
    }
}
