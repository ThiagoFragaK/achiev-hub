using achiev_hub.Server.Application.Common;
using achiev_hub.Server.Application.Games.Interfaces;
using achiev_hub.Server.Application.Common.Interfaces;
using achiev_hub.Server.Domain.Entities;

namespace achiev_hub.Server.Application.Games;

public class GameService : IGameService
{
    private readonly IRepository<Game> _games;

    public GameService(IRepository<Game> games)
    {
        _games = games;
    }

    public async Task<IReadOnlyList<GameDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException(
            "Listing all games is disabled. Query by id or use the library/Steam endpoints.");
    }

    public async Task<GameDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return Map(await GetRequiredAsync(id, cancellationToken));
    }

    public async Task<GameDto> CreateAsync(CreateGameRequest request, CancellationToken cancellationToken = default)
    {
        var steamId = SteamMediaUrls.RequireTrimmedKey(request.GameSteamId, nameof(request.GameSteamId));
        await EnsureSteamIdIsUniqueAsync(steamId, null, cancellationToken);

        var game = new Game
        {
            Name = request.Name.Trim(),
            ImageUrl = SteamMediaUrls.ToStorageHash(request.ImageUrl),
            GameSteamId = steamId,
            Developers = request.Developers,
            Publishers = request.Publishers
        };

        await _games.AddAsync(game, cancellationToken);
        await _games.SaveChangesAsync(cancellationToken);
        return Map(game);
    }

    public async Task<GameDto> UpdateAsync(int id, UpdateGameRequest request, CancellationToken cancellationToken = default)
    {
        var game = await GetRequiredAsync(id, cancellationToken);
        var steamId = SteamMediaUrls.RequireTrimmedKey(request.GameSteamId, nameof(request.GameSteamId));
        await EnsureSteamIdIsUniqueAsync(steamId, id, cancellationToken);

        game.Name = request.Name.Trim();
        game.ImageUrl = SteamMediaUrls.ToStorageHash(request.ImageUrl);
        game.GameSteamId = steamId;
        game.Developers = request.Developers;
        game.Publishers = request.Publishers;

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

    private async Task EnsureSteamIdIsUniqueAsync(string steamId, int? excludeId, CancellationToken cancellationToken)
    {
        var exists = await _games.AnyAsync(
            game => game.GameSteamId == steamId && (!excludeId.HasValue || game.Id != excludeId.Value),
            cancellationToken);

        if (exists)
        {
            throw new ConflictException("Game Steam ID is already in use.");
        }
    }

    private static GameDto Map(Game game) => new()
    {
        Id = game.Id,
        Name = game.Name,
        ImageUrl = SteamMediaUrls.AppIconUrl(game.GameSteamId, game.ImageUrl),
        GameSteamId = game.GameSteamId,
        Developers = game.Developers,
        Publishers = game.Publishers
    };
}
