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
[Route("api/users/stats")]
public class UsersStatsController : ApiControllerBase
{
    private readonly IUserStatsService _stats;

    public UsersStatsController(IUserStatsService stats)
    {
        _stats = stats;
    }

    [HttpGet]
    public async Task<ActionResult<UserStatsDto>> GetStats(CancellationToken cancellationToken)
    {
        // Guests have no rows in the database, so they get an empty (zeroed) set.
        if (ResolveUserIdForDbReads() is not int userId)
        {
            return Ok(_stats.GetEmptyStats());
        }

        return Ok(await _stats.GetStatsAsync(userId, cancellationToken));
    }

    [HttpGet("games/{appId:int}")]
    public async Task<ActionResult<GameProgressDto>> GetGameProgress(int appId, CancellationToken cancellationToken)
    {
        if (ResolveUserIdForDbReads() is not int userId)
        {
            return Ok(_stats.GetEmptyGameProgress());
        }

        return Ok(await _stats.GetGameProgressAsync(userId, appId, cancellationToken));
    }
}
