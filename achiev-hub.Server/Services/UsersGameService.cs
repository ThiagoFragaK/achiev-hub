using achiev_hub.Server.DTOs.Persistence;
using achiev_hub.Server.Entities;
using achiev_hub.Server.Exceptions;
using achiev_hub.Server.Repositories.Interfaces;
using achiev_hub.Server.Services.Interfaces;

namespace achiev_hub.Server.Services;

public class UsersGameService : IUsersGameService
{
    private readonly IRepository<UsersGame> _usersGames;
    private readonly IRepository<User> _users;
    private readonly IRepository<Game> _games;

    public UsersGameService(IRepository<UsersGame> usersGames, IRepository<User> users, IRepository<Game> games)
    {
        _usersGames = usersGames;
        _users = users;
        _games = games;
    }

    public async Task<IReadOnlyList<UsersGameDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var items = await _usersGames.GetAllAsync(cancellationToken);
        return items.Select(Map).ToList();
    }

    public async Task<UsersGameDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return Map(await GetRequiredAsync(id, cancellationToken));
    }

    public async Task<UsersGameDto> CreateAsync(CreateUsersGameRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureForeignKeysAsync(request.UserId, request.GameId, cancellationToken);
        await EnsureUniquePairAsync(request.UserId, request.GameId, null, cancellationToken);

        var item = new UsersGame
        {
            GameId = request.GameId,
            UserId = request.UserId,
            AchievementsPercentage = request.AchievementsPercentage
        };

        await _usersGames.AddAsync(item, cancellationToken);
        await _usersGames.SaveChangesAsync(cancellationToken);
        return Map(item);
    }

    public async Task<UsersGameDto> UpdateAsync(int id, UpdateUsersGameRequest request, CancellationToken cancellationToken = default)
    {
        var item = await GetRequiredAsync(id, cancellationToken);
        await EnsureForeignKeysAsync(request.UserId, request.GameId, cancellationToken);
        await EnsureUniquePairAsync(request.UserId, request.GameId, id, cancellationToken);

        item.GameId = request.GameId;
        item.UserId = request.UserId;
        item.AchievementsPercentage = request.AchievementsPercentage;

        _usersGames.Update(item);
        await _usersGames.SaveChangesAsync(cancellationToken);
        return Map(item);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var item = await GetRequiredAsync(id, cancellationToken);
        _usersGames.Remove(item);
        await _usersGames.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureForeignKeysAsync(int userId, int gameId, CancellationToken cancellationToken)
    {
        if (!await _users.AnyAsync(user => user.Id == userId, cancellationToken))
        {
            throw new NotFoundException($"User {userId} was not found.");
        }

        if (!await _games.AnyAsync(game => game.Id == gameId, cancellationToken))
        {
            throw new NotFoundException($"Game {gameId} was not found.");
        }
    }

    private async Task EnsureUniquePairAsync(int userId, int gameId, int? excludeId, CancellationToken cancellationToken)
    {
        var exists = await _usersGames.AnyAsync(
            item => item.UserId == userId && item.GameId == gameId && (!excludeId.HasValue || item.Id != excludeId.Value),
            cancellationToken);

        if (exists)
        {
            throw new ConflictException("This user already has this game.");
        }
    }

    private async Task<UsersGame> GetRequiredAsync(int id, CancellationToken cancellationToken)
    {
        return await _usersGames.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Users-game {id} was not found.");
    }

    private static UsersGameDto Map(UsersGame item) => new()
    {
        Id = item.Id,
        GameId = item.GameId,
        UserId = item.UserId,
        AchievementsPercentage = item.AchievementsPercentage
    };
}
