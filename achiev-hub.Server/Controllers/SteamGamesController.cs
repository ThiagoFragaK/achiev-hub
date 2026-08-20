using achiev_hub.Server.DTOs;
using achiev_hub.Server.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace achiev_hub.Server.Controllers;

[ApiController]
[Route("api/steam/games")]
public class SteamGamesController : ControllerBase
{
    private readonly IGamesService _gamesService;

    public SteamGamesController(IGamesService gamesService)
    {
        _gamesService = gamesService;
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

        var result = await _gamesService.GetRecentGamesAsync(steamId, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet]
    public async Task<ActionResult<PagedResultDto<LibraryGameDto>>> GetLibrary(
        [FromQuery] string steamId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(steamId))
        {
            return BadRequest("steamId is required.");
        }

        var result = await _gamesService.GetLibraryAsync(steamId, page, pageSize, cancellationToken);
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
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(steamId))
        {
            return BadRequest("steamId is required.");
        }

        var result = await _gamesService.GetAchievementsAsync(steamId, appId, page, pageSize, cancellationToken);
        return Ok(result);
    }
}
