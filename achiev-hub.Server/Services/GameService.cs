using achiev_hub.Server.DTOs.Persistence;
using achiev_hub.Server.Entities;
using achiev_hub.Server.Exceptions;
using achiev_hub.Server.Repositories.Interfaces;
using achiev_hub.Server.Services.Interfaces;

namespace achiev_hub.Server.Services;

public class GameService : IGameService
{
    private readonly IRepository<Game> _games;

    public GameService(IRepository<Game> games)
    {
        _games = games;
    }

    public async Task<IReadOnlyList<GameDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var games = await _games.GetAllAsync(cancellationToken);
        return games.Select(Map).ToList();
    }

    public async Task<GameDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return Map(await GetRequiredAsync(id, cancellationToken));
    }

    public async Task<GameDto> CreateAsync(CreateGameRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureSteamIdIsUniqueAsync(request.GameSteamId, null, cancellationToken);

        var game = new Game
        {
            Name = request.Name.Trim(),
            ImageUrl = request.ImageUrl,
            GameSteamId = NormalizeSteamId(request.GameSteamId),
            PlayTime = request.PlayTime
        };

        await _games.AddAsync(game, cancellationToken);
        await _games.SaveChangesAsync(cancellationToken);
        return Map(game);
    }

    public async Task<GameDto> UpdateAsync(int id, UpdateGameRequest request, CancellationToken cancellationToken = default)
    {
        var game = await GetRequiredAsync(id, cancellationToken);
        await EnsureSteamIdIsUniqueAsync(request.GameSteamId, id, cancellationToken);

        game.Name = request.Name.Trim();
        game.ImageUrl = request.ImageUrl;
        game.GameSteamId = NormalizeSteamId(request.GameSteamId);
        game.PlayTime = request.PlayTime;

        _games.Update(game);
        await _games.SaveChangesAsync(cancellationToken);
        return Map(game);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var game = await GetRequiredAsync(id, cancellationToken);
        _games.Remove(game);
        await _games.SaveChangesAsync(cancellationToken);
    }

    private async Task<Game> GetRequiredAsync(int id, CancellationToken cancellationToken)
    {
        return await _games.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Game {id} was not found.");
    }

    private async Task EnsureSteamIdIsUniqueAsync(string? steamId, int? excludeId, CancellationToken cancellationToken)
    {
        var normalized = NormalizeSteamId(steamId);
        if (normalized is null)
        {
            return;
        }

        var exists = await _games.AnyAsync(
            game => game.GameSteamId == normalized && (!excludeId.HasValue || game.Id != excludeId.Value),
            cancellationToken);

        if (exists)
        {
            throw new ConflictException("Game Steam ID is already in use.");
        }
    }

    private static string? NormalizeSteamId(string? steamId)
    {
        return string.IsNullOrWhiteSpace(steamId) ? null : steamId.Trim();
    }

    private static GameDto Map(Game game) => new()
    {
        Id = game.Id,
        Name = game.Name,
        ImageUrl = game.ImageUrl,
        GameSteamId = game.GameSteamId,
        PlayTime = game.PlayTime
    };
}
