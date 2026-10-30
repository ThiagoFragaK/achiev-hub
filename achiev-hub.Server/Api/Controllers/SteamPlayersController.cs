using achiev_hub.Server.Application.Steam;
using achiev_hub.Server.Application.Steam.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace achiev_hub.Server.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/steam/players")]
[EnableRateLimiting("steam")]
public class SteamPlayersController : ApiControllerBase
{
    private readonly IPlayersService _playersService;

    public SteamPlayersController(IPlayersService playersService)
    {
        _playersService = playersService;
    }

    [HttpGet("{steamId}")]
    public async Task<ActionResult<PlayerDto>> GetPlayer(string steamId, CancellationToken cancellationToken)
    {
        if (ForbidGuests("Guests cannot look up Steam players.") is { } guestDenied)
        {
            return guestDenied;
        }

        if (TryResolveBoundSteamId(steamId, out var boundSteamId) is { } denied)
        {
            return denied;
        }

        var player = await _playersService.GetPlayerAsync(boundSteamId, cancellationToken);
        return player is null ? NotFound() : Ok(player);
    }
}
