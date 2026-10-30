using achiev_hub.Server.Application.Users;
using achiev_hub.Server.Application.Users.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace achiev_hub.Server.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/users-games")]
public class UsersGamesController : ApiControllerBase
{
    private readonly IUsersGameService _usersGames;

    public UsersGamesController(IUsersGameService usersGames)
    {
        _usersGames = usersGames;
    }

    [Authorize(Roles = "admin")]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UsersGameDto>>> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await _usersGames.GetAllAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UsersGameDto>> GetById(int id, CancellationToken cancellationToken)
    {
        try
        {
            var item = await _usersGames.GetByIdAsync(id, cancellationToken);
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
    public async Task<ActionResult<UsersGameDto>> Create(CreateUsersGameRequest request, CancellationToken cancellationToken)
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
            var created = await _usersGames.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<UsersGameDto>> Update(int id, UpdateUsersGameRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var existing = await _usersGames.GetByIdAsync(id, cancellationToken);
            if (ForbidUnlessSelfOrAdmin(existing.UserId) is { } denied)
            {
                return denied;
            }

            if (!IsAdmin())
            {
                request.UserId = existing.UserId;
            }

            return Ok(await _usersGames.UpdateAsync(id, request, cancellationToken));
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
            var existing = await _usersGames.GetByIdAsync(id, cancellationToken);
            if (ForbidUnlessSelfOrAdmin(existing.UserId) is { } denied)
            {
                return denied;
            }

            await _usersGames.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }
}
