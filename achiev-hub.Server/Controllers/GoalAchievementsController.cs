using achiev_hub.Server.DTOs.Persistence;
using achiev_hub.Server.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace achiev_hub.Server.Controllers;

[ApiController]
[Route("api/goal-achievements")]
public class GoalAchievementsController : ApiControllerBase
{
    private readonly IGoalAchievementService _goalAchievements;

    public GoalAchievementsController(IGoalAchievementService goalAchievements)
    {
        _goalAchievements = goalAchievements;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<GoalAchievementDto>>> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await _goalAchievements.GetAllAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<GoalAchievementDto>> GetById(int id, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _goalAchievements.GetByIdAsync(id, cancellationToken));
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPost]
    public async Task<ActionResult<GoalAchievementDto>> Create(CreateGoalAchievementRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var created = await _goalAchievements.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<GoalAchievementDto>> Update(int id, UpdateGoalAchievementRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _goalAchievements.UpdateAsync(id, request, cancellationToken));
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        try
        {
            await _goalAchievements.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }
}
