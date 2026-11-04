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
    private readonly IUserStatsService _userStatsService;

    public UsersStatsController(IUserStatsService UserStatsService)
    {
        _userStatsService = UserStatsService;
    }

    [HttpGet]
    public async Task<ActionResult<UserStatsDto>> GetStats(CancellationToken cancellationToken)
    {
        if (ResolveUserIdForDbReads() is not int userId)
        {
            return Ok(_userStatsService.GetEmptyStats());
        }

        return Ok(await _userStatsService.GetStatsAsync(userId, cancellationToken));
    }

    [HttpGet("games/{appId:int}")]
    public async Task<ActionResult<GameProgressDto>> GetGameProgress(int appId, CancellationToken cancellationToken)
    {
        if (ResolveUserIdForDbReads() is not int userId)
        {
            return Ok(_userStatsService.GetEmptyGameProgress());
        }

        return Ok(await _userStatsService.GetGameProgressAsync(userId, appId, cancellationToken));
    }

    [HttpGet("achievements-by-month")]
    public async Task<ActionResult<AchievementsByMonthDto>> GetAchievementsByMonth(CancellationToken cancellationToken)
    {
        if (ResolveUserIdForDbReads() is not int userId)
        {
            return Ok(_userStatsService.GetEmptyAchievementsByMonth());
        }

        return Ok(await _userStatsService.GetAchievementsByMonthAsync(userId, cancellationToken));
    }

    [HttpGet("games-percentage-bands")]
    public async Task<ActionResult<GamesPercentageBandsDto>> GetGamesPercentageBands(CancellationToken cancellationToken)
    {
        if (ResolveUserIdForDbReads() is not int userId)
        {
            return Ok(_userStatsService.GetEmptyGamesPercentageBands());
        }

        return Ok(await _userStatsService.GetGamesPercentageBandsAsync(userId, cancellationToken));
    }

    [HttpGet("games-for-average")]
    public async Task<ActionResult<GamesForAverageDto>> GetGamesForAverage(CancellationToken cancellationToken)
    {
        if (ResolveUserIdForDbReads() is not int userId)
        {
            return Ok(_userStatsService.GetEmptyGamesForAverage());
        }

        return Ok(await _userStatsService.GetGamesForAverageAsync(userId, cancellationToken));
    }
}
