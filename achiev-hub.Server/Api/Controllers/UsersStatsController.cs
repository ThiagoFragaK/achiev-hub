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
        if (ResolveUserId() is not int userId)
        {
            return Ok(_userStatsService.GetEmptyStats());
        }

        return Ok(await _userStatsService.GetStatsAsync(userId, cancellationToken));
    }

    [HttpGet("games/{appId:int}")]
    public async Task<ActionResult<GameProgressDto>> GetGameProgress(int appId, CancellationToken cancellationToken)
    {
        if (ResolveUserId() is not int userId)
        {
            return Ok(_userStatsService.GetEmptyGameProgress());
        }

        return Ok(await _userStatsService.GetGameProgressAsync(userId, appId, cancellationToken));
    }

    [HttpGet("achievements-by-month")]
    public async Task<ActionResult<AchievementsByMonthDto>> GetAchievementsByMonth(CancellationToken cancellationToken)
    {
        if (ResolveUserId() is not int userId)
        {
            return Ok(_userStatsService.GetEmptyAchievementsByMonth());
        }

        return Ok(await _userStatsService.GetAchievementsByMonthAsync(userId, cancellationToken));
    }

    [HttpGet("achievements-by-month/{year:int}/{month:int}")]
    public async Task<ActionResult<PagedResultDto<RecentAchievementDto>>> GetAchievementsForMonth(
        int year,
        int month,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? name = null,
        CancellationToken cancellationToken = default)
    {
        if (month is < 1 or > 12)
        {
            return BadRequest("Month must be between 1 and 12.");
        }

        var size = pageSize <= 0 ? 10 : Math.Min(pageSize, 10);

        if (ResolveUserId() is not int userId)
        {
            return Ok(PagedResultDto<RecentAchievementDto>.Empty(size));
        }

        return Ok(await _userStatsService.GetAchievementsForMonthAsync(
            userId,
            year,
            month,
            page,
            size,
            name,
            cancellationToken));
    }

    [HttpGet("games-percentage-bands")]
    public async Task<ActionResult<GamesPercentageBandsDto>> GetGamesPercentageBands(CancellationToken cancellationToken)
    {
        if (ResolveUserId() is not int userId)
        {
            return Ok(_userStatsService.GetEmptyGamesPercentageBands());
        }

        return Ok(await _userStatsService.GetGamesPercentageBandsAsync(userId, cancellationToken));
    }

    [HttpGet("games-percentage-band-games")]
    public async Task<ActionResult<GamesPercentageBandGamesDto>> GetGamesPercentageBandGames(CancellationToken cancellationToken)
    {
        if (ResolveUserId() is not int userId)
        {
            return Ok(_userStatsService.GetEmptyGamesPercentageBandGames());
        }

        return Ok(await _userStatsService.GetGamesPercentageBandGamesAsync(userId, cancellationToken));
    }

    [HttpGet("games-for-average")]
    public async Task<ActionResult<GamesForAverageDto>> GetGamesForAverage(CancellationToken cancellationToken)
    {
        if (ResolveUserId() is not int userId)
        {
            return Ok(_userStatsService.GetEmptyGamesForAverage());
        }

        return Ok(await _userStatsService.GetGamesForAverageAsync(userId, cancellationToken));
    }

    [HttpGet("completed-games-by-year")]
    public async Task<ActionResult<CompletedGamesByYearDto>> GetCompletedGamesByYear(CancellationToken cancellationToken)
    {
        if (ResolveUserId() is not int userId)
        {
            return Ok(_userStatsService.GetEmptyCompletedGamesByYear());
        }

        return Ok(await _userStatsService.GetCompletedGamesByYearAsync(userId, cancellationToken));
    }
}
