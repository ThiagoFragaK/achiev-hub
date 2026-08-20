using achiev_hub.Server.DTOs.Persistence;
using achiev_hub.Server.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace achiev_hub.Server.Controllers;

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
    public async Task<ActionResult<IReadOnlyList<GoalDto>>> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await _goals.GetAllAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<GoalDto>> GetById(int id, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _goals.GetByIdAsync(id, cancellationToken));
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPost]
    public async Task<ActionResult<GoalDto>> Create(CreateGoalRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var created = await _goals.CreateAsync(request, cancellationToken);
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
        try
        {
            return Ok(await _goals.UpdateAsync(id, request, cancellationToken));
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
            await _goals.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }
}
