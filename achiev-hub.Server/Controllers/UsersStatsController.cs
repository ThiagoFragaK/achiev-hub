using achiev_hub.Server.DTOs;
using achiev_hub.Server.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace achiev_hub.Server.Controllers;

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
