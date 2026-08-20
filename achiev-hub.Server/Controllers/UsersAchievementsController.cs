using achiev_hub.Server.DTOs.Persistence;
using achiev_hub.Server.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace achiev_hub.Server.Controllers;

[ApiController]
[Route("api/users-achievements")]
public class UsersAchievementsController : ApiControllerBase
{
    private readonly IUsersAchievementService _usersAchievements;

    public UsersAchievementsController(IUsersAchievementService usersAchievements)
    {
        _usersAchievements = usersAchievements;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UsersAchievementDto>>> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await _usersAchievements.GetAllAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UsersAchievementDto>> GetById(int id, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _usersAchievements.GetByIdAsync(id, cancellationToken));
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPost]
    public async Task<ActionResult<UsersAchievementDto>> Create(CreateUsersAchievementRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var created = await _usersAchievements.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<UsersAchievementDto>> Update(int id, UpdateUsersAchievementRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _usersAchievements.UpdateAsync(id, request, cancellationToken));
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
            await _usersAchievements.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }
}
