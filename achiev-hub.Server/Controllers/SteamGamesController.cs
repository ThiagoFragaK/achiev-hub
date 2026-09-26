using achiev_hub.Server.DTOs;
using achiev_hub.Server.Enums;
using achiev_hub.Server.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace achiev_hub.Server.Controllers;

[Authorize]
[EnableRateLimiting("steam")]
[ApiController]
[Route("api/steam/games")]
public class SteamGamesController : ApiControllerBase
{
    private readonly IGamesService _gamesService;
    private readonly ISyncJobEnqueueService _enqueueService;

    public SteamGamesController(IGamesService gamesService, ISyncJobEnqueueService enqueueService)
    {
        _gamesService = gamesService;
        _enqueueService = enqueueService;
    }

    [HttpGet("recent")]
    public async Task<ActionResult<PagedResultDto<RecentGameDto>>> GetRecentGames(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 7,
        CancellationToken cancellationToken = default)
    {
        if (RequireSteamId(out var steamId) is { } error)
        {
            return error;
        }

        var result = await _gamesService.GetRecentGamesAsync(
            steamId,
            page,
            pageSize,
            cancellationToken: cancellationToken);
        return Ok(result);
    }

    [HttpGet]
    public async Task<ActionResult<PagedResultDto<LibraryGameDto>>> GetLibrary(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? name = null,
        [FromQuery] double? minHours = null,
        [FromQuery] bool? hasAchievements = null,
        CancellationToken cancellationToken = default)
    {
        if (RequireSteamId(out var steamId) is { } error)
        {
            return error;
        }

        var filters = new LibraryGameFilterDto
        {
            Name = name,
            MinHours = minHours,
            HasAchievements = hasAchievements
        };

        var result = await _gamesService.GetLibraryAsync(
            steamId,
            page,
            pageSize,
            ResolveUserIdForDbReads(),
            filters,
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("{appId:int}")]
    public async Task<ActionResult<GameDetailsDto>> GetGameDetails(int appId, CancellationToken cancellationToken)
    {
        var details = await _gamesService.GetGameDetailsAsync(
            appId,
            ResolveUserIdForDbReads(),
            cancellationToken);
        return details is null ? NotFound() : Ok(details);
    }

    [HttpGet("{appId:int}/achievements")]
    public async Task<ActionResult<PagedResultDto<AchievementDto>>> GetAchievements(
        int appId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? name = null,
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        if (RequireSteamId(out var steamId) is { } error)
        {
            return error;
        }

        try
        {
            var result = await _gamesService.GetAchievementsAsync(
                steamId,
                appId,
                page,
                pageSize,
                name,
                status,
                ResolveUserIdForDbReads(),
                cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPost("{appId:int}/sync-achievements")]
    public async Task<ActionResult<EnqueueSyncResponseDto>> SyncGameAchievements(
        int appId,
        CancellationToken cancellationToken)
    {
        if (IsGuest())
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Guests cannot sync achievements to the database." });
        }

        if (ResolveUserId() is not int userId)
        {
            return Unauthorized(new { message = "Invalid token" });
        }

        if (RequireSteamId(out var steamId) is { } error)
        {
            return error;
        }

        var jobId = await _enqueueService.EnqueueAsync(
            SyncJobType.AchievementGame,
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

    [HttpPost("sync")]
    public async Task<ActionResult<EnqueueSyncResponseDto>> SyncLibrary(CancellationToken cancellationToken)
    {
        if (IsGuest())
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Guests cannot sync library to the database." });
        }

        if (ResolveUserId() is not int userId)
        {
            return Unauthorized(new { message = "Invalid token" });
        }

        if (RequireSteamId(out var steamId) is { } error)
        {
            return error;
        }

        var jobIds = await _enqueueService.EnqueueManualLibrarySyncAsync(userId, steamId, cancellationToken);
        return Accepted(new EnqueueSyncResponseDto
        {
            Message = "Library sync enqueued.",
            JobIds = jobIds
        });
    }
}
