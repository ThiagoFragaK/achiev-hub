using achiev_hub.Server.DTOs.Persistence;
using achiev_hub.Server.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace achiev_hub.Server.Controllers;

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
