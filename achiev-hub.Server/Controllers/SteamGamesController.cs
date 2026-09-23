using System.Security.Claims;
using achiev_hub.Server.DTOs;
using achiev_hub.Server.Services;
using achiev_hub.Server.Services.Interfaces;
using achiev_hub.Server.Support;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace achiev_hub.Server.Controllers;

[Authorize]
[ApiController]
[Route("api/steam/games")]
public class SteamGamesController : ApiControllerBase
{
    private readonly IGamesService _gamesService;
    private readonly ISteamSyncService _steamSyncService;

    public SteamGamesController(IGamesService gamesService, ISteamSyncService steamSyncService)
    {
        _gamesService = gamesService;
        _steamSyncService = steamSyncService;
    }

    [HttpGet("recent")]
    public async Task<ActionResult<PagedResultDto<RecentGameDto>>> GetRecentGames(
        [FromQuery] string steamId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 7,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(steamId))
        {
            return BadRequest("steamId is required.");
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
        [FromQuery] string steamId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? name = null,
        [FromQuery] double? minHours = null,
        [FromQuery] bool? hasAchievements = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(steamId))
        {
            return BadRequest("steamId is required.");
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
        var details = await _gamesService.GetGameDetailsAsync(appId, cancellationToken);
        return details is null ? NotFound() : Ok(details);
    }

    [HttpGet("{appId:int}/achievements")]
    public async Task<ActionResult<PagedResultDto<AchievementDto>>> GetAchievements(
        int appId,
        [FromQuery] string steamId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? name = null,
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(steamId))
        {
            return BadRequest("steamId is required.");
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
    public async Task<IActionResult> SyncGameAchievements(int appId, CancellationToken cancellationToken)
    {
        if (IsGuest())
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Guests cannot sync achievements to the database." });
        }

        if (ResolveUserId() is not int userId)
        {
            return Unauthorized(new { message = "Invalid token" });
        }

        var steamId = User.FindFirstValue("steam_id");
        if (string.IsNullOrWhiteSpace(steamId))
        {
            return BadRequest(new { message = "Steam ID is missing from the token." });
        }

        try
        {
            await _steamSyncService.SyncGameAchievementsAsync(userId, steamId, appId, cancellationToken);
            return Ok(new { success = true, message = "Achievements synced." });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = ex.Message });
        }
    }

    [HttpPost("sync")]
    public async Task<IActionResult> SyncLibrary(CancellationToken cancellationToken)
    {
        if (IsGuest())
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Guests cannot sync library to the database." });
        }

        if (ResolveUserId() is not int userId)
        {
            return Unauthorized(new { message = "Invalid token" });
        }

        var steamId = User.FindFirstValue("steam_id");
        if (string.IsNullOrWhiteSpace(steamId))
        {
            return BadRequest(new { message = "Steam ID is missing from the token." });
        }

        try
        {
            await _steamSyncService.SyncLibraryAsync(
                userId,
                steamId,
                LibrarySyncScope.Full,
                cancellationToken);
            await _steamSyncService.SyncAchievementsForUserAsync(
                userId,
                steamId,
                AchievementSyncScope.AllOwnedWithStats,
                cancellationToken);
            return Ok(new { success = true, message = "Library synced." });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = ex.Message });
        }
    }
}
