using achiev_hub.Server.Application.Games;
using achiev_hub.Server.Application.Games.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace achiev_hub.Server.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/games")]
public class GamesController : ApiControllerBase
{
    private readonly IGameService _games;

    public GamesController(IGameService games)
    {
        _games = games;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<GameDto>>> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await _games.GetAllAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<GameDto>> GetById(int id, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _games.GetByIdAsync(id, cancellationToken));
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [Authorize(Roles = "admin")]
    [HttpPost]
    public async Task<ActionResult<GameDto>> Create(CreateGameRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var created = await _games.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [Authorize(Roles = "admin")]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<GameDto>> Update(int id, UpdateGameRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _games.UpdateAsync(id, request, cancellationToken));
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [Authorize(Roles = "admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        try
        {
            await _games.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }
}
