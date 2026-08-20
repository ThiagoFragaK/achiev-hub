using achiev_hub.Server.DTOs;
using achiev_hub.Server.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace achiev_hub.Server.Controllers;

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
