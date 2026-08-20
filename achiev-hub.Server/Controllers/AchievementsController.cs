using achiev_hub.Server.DTOs.Persistence;
using achiev_hub.Server.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace achiev_hub.Server.Controllers;

[ApiController]
[Route("api/achievements")]
public class AchievementsController : ApiControllerBase
{
    private readonly IAchievementService _achievements;

    public AchievementsController(IAchievementService achievements)
    {
        _achievements = achievements;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AchievementRecordDto>>> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await _achievements.GetAllAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AchievementRecordDto>> GetById(int id, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _achievements.GetByIdAsync(id, cancellationToken));
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPost]
    public async Task<ActionResult<AchievementRecordDto>> Create(CreateAchievementRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var created = await _achievements.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<AchievementRecordDto>> Update(int id, UpdateAchievementRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _achievements.UpdateAsync(id, request, cancellationToken));
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
            await _achievements.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }
}
