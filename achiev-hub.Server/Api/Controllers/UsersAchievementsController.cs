using achiev_hub.Server.Application.Users;
using achiev_hub.Server.Application.Users.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace achiev_hub.Server.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/users-achievements")]
public class UsersAchievementsController : ApiControllerBase
{
    private readonly IUsersAchievementService _usersAchievements;

    public UsersAchievementsController(IUsersAchievementService usersAchievements)
    {
        _usersAchievements = usersAchievements;
    }

    [Authorize(Roles = "admin")]
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
            var item = await _usersAchievements.GetByIdAsync(id, cancellationToken);
            if (ForbidUnlessSelfOrAdmin(item.UserId) is { } denied)
            {
                return denied;
            }

            return Ok(item);
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPost]
    public async Task<ActionResult<UsersAchievementDto>> Create(CreateUsersAchievementRequest request, CancellationToken cancellationToken)
    {
        if (ResolveUserId() is not int userId)
        {
            return Unauthorized();
        }

        if (!IsAdmin())
        {
            request.UserId = userId;
        }

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
            var existing = await _usersAchievements.GetByIdAsync(id, cancellationToken);
            if (ForbidUnlessSelfOrAdmin(existing.UserId) is { } denied)
            {
                return denied;
            }

            if (!IsAdmin())
            {
                request.UserId = existing.UserId;
            }

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
            var existing = await _usersAchievements.GetByIdAsync(id, cancellationToken);
            if (ForbidUnlessSelfOrAdmin(existing.UserId) is { } denied)
            {
                return denied;
            }

            await _usersAchievements.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }
}
