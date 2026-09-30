using achiev_hub.Server.Application.Games;
using achiev_hub.Server.Application.Games.Interfaces;
using achiev_hub.Server.Application.Achievements;
using achiev_hub.Server.Application.Achievements.Interfaces;
using achiev_hub.Server.Application.Steam;
using achiev_hub.Server.Application.Steam.Interfaces;
using achiev_hub.Server.Application.Stats;
using achiev_hub.Server.Application.Stats.Interfaces;
using achiev_hub.Server.Application.Users;
using achiev_hub.Server.Application.Users.Interfaces;
using achiev_hub.Server.Application.Goals;
using achiev_hub.Server.Application.Goals.Interfaces;
using achiev_hub.Server.Application.Auth;
using achiev_hub.Server.Application.Auth.Interfaces;
using achiev_hub.Server.Application.Common;
using achiev_hub.Server.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace achiev_hub.Server.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/steam/players")]
public class SteamPlayersController : ControllerBase
{
    private readonly IPlayersService _playersService;

    public SteamPlayersController(IPlayersService playersService)
    {
        _playersService = playersService;
    }

    [HttpGet("{steamId}")]
    public async Task<ActionResult<PlayerDto>> GetPlayer(string steamId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(steamId))
        {
            return BadRequest("steamId is required.");
        }

        var player = await _playersService.GetPlayerAsync(steamId, cancellationToken);
        return player is null ? NotFound() : Ok(player);
    }
}
