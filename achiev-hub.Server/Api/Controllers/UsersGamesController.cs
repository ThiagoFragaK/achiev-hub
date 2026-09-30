using achiev_hub.Server.Application.Users;
using achiev_hub.Server.Application.Users.Interfaces;
using achiev_hub.Server.Application.Games;
using achiev_hub.Server.Application.Games.Interfaces;
using achiev_hub.Server.Application.Achievements;
using achiev_hub.Server.Application.Achievements.Interfaces;
using achiev_hub.Server.Application.Goals;
using achiev_hub.Server.Application.Goals.Interfaces;
using achiev_hub.Server.Application.Auth;
using achiev_hub.Server.Application.Auth.Interfaces;
using achiev_hub.Server.Application.Steam;
using achiev_hub.Server.Application.Steam.Interfaces;
using achiev_hub.Server.Application.Stats;
using achiev_hub.Server.Application.Stats.Interfaces;
using achiev_hub.Server.Application.Common;
using achiev_hub.Server.Application.Common.Interfaces;
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
            return Ok(await _usersGames.GetByIdAsync(id, cancellationToken));
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPost]
    public async Task<ActionResult<UsersGameDto>> Create(CreateUsersGameRequest request, CancellationToken cancellationToken)
    {
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
            await _usersGames.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }
}
