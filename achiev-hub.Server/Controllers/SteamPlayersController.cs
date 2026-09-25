using achiev_hub.Server.DTOs;
using achiev_hub.Server.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace achiev_hub.Server.Controllers;

[Authorize]
[EnableRateLimiting("steam")]
[ApiController]
[Route("api/steam/players")]
public class SteamPlayersController : ApiControllerBase
{
    private readonly IPlayersService _playersService;

    public SteamPlayersController(IPlayersService playersService)
    {
        _playersService = playersService;
    }

    [HttpGet]
    public async Task<ActionResult<PlayerDto>> GetPlayer(CancellationToken cancellationToken)
    {
        if (RequireSteamId(out var steamId) is { } error)
        {
            return error;
        }

        var player = await _playersService.GetPlayerAsync(steamId, cancellationToken);
        return player is null ? NotFound() : Ok(player);
    }
}
