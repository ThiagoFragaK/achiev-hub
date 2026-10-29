using achiev_hub.Server.Application.Goals;
using achiev_hub.Server.Application.Goals.Interfaces;
using achiev_hub.Server.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace achiev_hub.Server.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/goals")]
public class GoalsController : ApiControllerBase
{
    private readonly IGoalService _goals;

    public GoalsController(IGoalService goals)
    {
        _goals = goals;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<GoalDto>>> GetAll(
        [FromQuery] GoalStatus? status,
        CancellationToken cancellationToken)
    {
        if (ResolveUserId() is not int userId || IsGuest())
        {
            return Ok(Array.Empty<GoalDto>());
        }

        return Ok(await _goals.GetForUserAsync(userId, status, cancellationToken));
    }

    [HttpGet("eligible-games")]
    public async Task<ActionResult<IReadOnlyList<EligibleGameDto>>> GetEligibleGames(
        [FromQuery] string? q,
        CancellationToken cancellationToken)
    {
        if (ResolveUserId() is not int userId || IsGuest())
        {
            return Ok(Array.Empty<EligibleGameDto>());
        }

        return Ok(await _goals.GetEligibleGamesAsync(userId, q, cancellationToken));
    }

    [HttpGet("completed-series")]
    public async Task<ActionResult<GoalsCompletedSeriesDto>> GetCompletedSeries(CancellationToken cancellationToken)
    {
        if (ResolveUserId() is not int userId || IsGuest())
        {
            return Ok(new GoalsCompletedSeriesDto());
        }

        return Ok(await _goals.GetCompletedSeriesAsync(userId, cancellationToken));
    }

    [HttpGet("unacknowledged-completed")]
    public async Task<ActionResult<IReadOnlyList<UnacknowledgedCompletedGoalDto>>> GetUnacknowledgedCompleted(
        CancellationToken cancellationToken)
    {
        if (ResolveUserId() is not int userId || IsGuest())
        {
            return Ok(Array.Empty<UnacknowledgedCompletedGoalDto>());
        }

        return Ok(await _goals.GetUnacknowledgedCompletedAsync(userId, cancellationToken));
    }

    [HttpPost("acknowledge")]
    public async Task<IActionResult> Acknowledge(CancellationToken cancellationToken)
    {
        if (ResolveUserId() is not int userId || IsGuest())
        {
            return Unauthorized();
        }

        try
        {
            await _goals.AcknowledgeCompletedAsync(userId, cancellationToken);
            return NoContent();
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpGet("by-game/{appId:int}")]
    public async Task<ActionResult<GoalDto>> GetActiveByGame(int appId, CancellationToken cancellationToken)
    {
        if (ResolveUserId() is not int userId || IsGuest())
        {
            return NotFound(new { message = "No active goal for this game." });
        }

        var goal = await _goals.GetActiveByAppIdAsync(userId, appId, cancellationToken);
        if (goal is null)
        {
            return NotFound(new { message = "No active goal for this game." });
        }

        return Ok(goal);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<GoalDto>> GetById(int id, CancellationToken cancellationToken)
    {
        if (ResolveUserId() is not int userId || IsGuest())
        {
            return Unauthorized();
        }

        try
        {
            return Ok(await _goals.GetByIdAsync(userId, id, cancellationToken));
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPost]
    public async Task<ActionResult<GoalDto>> Create(CreateGoalRequest request, CancellationToken cancellationToken)
    {
        if (ResolveUserId() is not int userId || IsGuest())
        {
            return Unauthorized();
        }

        try
        {
            var created = await _goals.CreateAsync(userId, request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<GoalDto>> Update(int id, UpdateGoalRequest request, CancellationToken cancellationToken)
    {
        if (ResolveUserId() is not int userId || IsGuest())
        {
            return Unauthorized();
        }

        try
        {
            return Ok(await _goals.UpdateAsync(userId, id, request, cancellationToken));
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpGet("{id:int}/achievements")]
    public async Task<ActionResult<IReadOnlyList<GoalAchievementDetailDto>>> GetAchievements(
        int id,
        CancellationToken cancellationToken)
    {
        if (ResolveUserId() is not int userId || IsGuest())
        {
            return Unauthorized();
        }

        try
        {
            return Ok(await _goals.GetAchievementsAsync(userId, id, cancellationToken));
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPost("{id:int}/achievements")]
    public async Task<ActionResult<GoalDto>> AttachAchievements(
        int id,
        AttachGoalAchievementsRequest request,
        CancellationToken cancellationToken)
    {
        if (ResolveUserId() is not int userId || IsGuest())
        {
            return Unauthorized();
        }

        try
        {
            return Ok(await _goals.AttachAchievementsAsync(userId, id, request, cancellationToken));
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPut("{id:int}/achievements")]
    public async Task<ActionResult<GoalDto>> ReplaceAchievements(
        int id,
        ReplaceGoalAchievementsRequest request,
        CancellationToken cancellationToken)
    {
        if (ResolveUserId() is not int userId || IsGuest())
        {
            return Unauthorized();
        }

        try
        {
            return Ok(await _goals.ReplaceAchievementsAsync(userId, id, request, cancellationToken));
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPost("{id:int}/abandon")]
    public async Task<ActionResult<GoalDto>> Abandon(int id, CancellationToken cancellationToken)
    {
        if (ResolveUserId() is not int userId || IsGuest())
        {
            return Unauthorized();
        }

        try
        {
            return Ok(await _goals.AbandonAsync(userId, id, cancellationToken));
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPost("{id:int}/reopen")]
    public async Task<ActionResult<GoalDto>> Reopen(int id, CancellationToken cancellationToken)
    {
        if (ResolveUserId() is not int userId || IsGuest())
        {
            return Unauthorized();
        }

        try
        {
            return Ok(await _goals.ReopenAsync(userId, id, cancellationToken));
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        if (ResolveUserId() is not int userId || IsGuest())
        {
            return Unauthorized();
        }

        try
        {
            await _goals.DeleteAsync(userId, id, cancellationToken);
            return NoContent();
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }
}
