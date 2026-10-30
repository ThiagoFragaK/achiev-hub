using System.Security.Claims;
using achiev_hub.Server.Application.Achievements;
using achiev_hub.Server.Application.Games;
using achiev_hub.Server.Application.Games.Interfaces;
using achiev_hub.Server.Application.Steam;
using achiev_hub.Server.Application.Steam.Interfaces;
using achiev_hub.Server.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace achiev_hub.Server.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/steam/games")]
[EnableRateLimiting("steam")]
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
        [FromQuery] string? steamId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 7,
        CancellationToken cancellationToken = default)
    {
        if (ForbidGuests() is { } guestDenied)
        {
            return guestDenied;
        }

        if (TryResolveBoundSteamId(steamId, out var boundSteamId) is { } denied)
        {
            return denied;
        }

        var result = await _gamesService.GetRecentGamesAsync(
            boundSteamId,
            page,
            pageSize,
            cancellationToken: cancellationToken);
        return Ok(result);
    }

    [HttpGet("recent-achievements")]
    public async Task<ActionResult<PagedResultDto<RecentAchievementDto>>> GetRecentAchievements(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        if (ResolveUserIdForDbReads() is not int userId)
        {
            return Ok(new PagedResultDto<RecentAchievementDto>
            {
                Data = [],
                CurrentPage = 1,
                LastPage = 1,
                PerPage = pageSize <= 0 ? 10 : Math.Min(pageSize, 10),
                TotalCount = 0
            });
        }

        var result = await _gamesService.GetRecentAchievementsAsync(
            userId,
            page,
            pageSize,
            cancellationToken);
        return Ok(result);
    }

    [HttpGet]
    public async Task<ActionResult<PagedResultDto<LibraryGameDto>>> GetLibrary(
        [FromQuery] string? steamId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? name = null,
        [FromQuery] double? minHours = null,
        [FromQuery] bool? hasAchievements = null,
        [FromQuery] string? sort = null,
        CancellationToken cancellationToken = default)
    {
        if (ForbidGuests() is { } guestDenied)
        {
            return guestDenied;
        }

        if (TryResolveBoundSteamId(steamId, out var boundSteamId) is { } denied)
        {
            return denied;
        }

        var filters = new LibraryGameFilterDto
        {
            Name = name,
            MinHours = minHours,
            HasAchievements = hasAchievements,
            Sort = sort
        };

        var result = await _gamesService.GetLibraryAsync(
            boundSteamId,
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
        [FromQuery] string? steamId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? name = null,
        [FromQuery] string? status = null,
        [FromQuery] string? sort = null,
        [FromQuery] string? global = null,
        CancellationToken cancellationToken = default)
    {
        if (ForbidGuests() is { } guestDenied)
        {
            return guestDenied;
        }

        if (TryResolveBoundSteamId(steamId, out var boundSteamId) is { } denied)
        {
            return denied;
        }

        try
        {
            var result = await _gamesService.GetAchievementsAsync(
                boundSteamId,
                appId,
                page,
                pageSize,
                name,
                status,
                sort,
                global,
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
        catch (SteamSyncCooldownException ex)
        {
            return StatusCode(StatusCodes.Status429TooManyRequests, new
            {
                message = ex.Message,
                retryAfterSeconds = ex.RetryAfterSeconds
            });
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
