using achiev_hub.Server.DTOs.Persistence;
using achiev_hub.Server.Entities;
using achiev_hub.Server.Exceptions;
using achiev_hub.Server.Repositories.Interfaces;
using achiev_hub.Server.Services.Interfaces;

namespace achiev_hub.Server.Services;

public class UsersAchievementService : IUsersAchievementService
{
    private readonly IRepository<UsersAchievement> _usersAchievements;
    private readonly IRepository<User> _users;
    private readonly IRepository<Game> _games;
    private readonly IRepository<Achievement> _achievements;

    public UsersAchievementService(
        IRepository<UsersAchievement> usersAchievements,
        IRepository<User> users,
        IRepository<Game> games,
        IRepository<Achievement> achievements)
    {
        _usersAchievements = usersAchievements;
        _users = users;
        _games = games;
        _achievements = achievements;
    }

    public async Task<IReadOnlyList<UsersAchievementDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var items = await _usersAchievements.GetAllAsync(cancellationToken);
        return items.Select(Map).ToList();
    }

    public async Task<UsersAchievementDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return Map(await GetRequiredAsync(id, cancellationToken));
    }

    public async Task<UsersAchievementDto> CreateAsync(CreateUsersAchievementRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureForeignKeysAsync(request.UserId, request.GameId, request.AchievementId, cancellationToken);
        await EnsureUniquePairAsync(request.UserId, request.AchievementId, null, cancellationToken);

        var item = new UsersAchievement
        {
            AchievementId = request.AchievementId,
            UserId = request.UserId,
            GameId = request.GameId,
            UnlockDate = request.UnlockDate
        };

        await _usersAchievements.AddAsync(item, cancellationToken);
        await _usersAchievements.SaveChangesAsync(cancellationToken);
        return Map(item);
    }

    public async Task<UsersAchievementDto> UpdateAsync(int id, UpdateUsersAchievementRequest request, CancellationToken cancellationToken = default)
    {
        var item = await GetRequiredAsync(id, cancellationToken);
        await EnsureForeignKeysAsync(request.UserId, request.GameId, request.AchievementId, cancellationToken);
        await EnsureUniquePairAsync(request.UserId, request.AchievementId, id, cancellationToken);

        item.AchievementId = request.AchievementId;
        item.UserId = request.UserId;
        item.GameId = request.GameId;
        item.UnlockDate = request.UnlockDate;

        _usersAchievements.Update(item);
        await _usersAchievements.SaveChangesAsync(cancellationToken);
        return Map(item);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var item = await GetRequiredAsync(id, cancellationToken);
        _usersAchievements.Remove(item);
        await _usersAchievements.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureForeignKeysAsync(int userId, int gameId, int achievementId, CancellationToken cancellationToken)
    {
        if (!await _users.AnyAsync(user => user.Id == userId, cancellationToken))
        {
            throw new NotFoundException($"User {userId} was not found.");
        }

        if (!await _games.AnyAsync(game => game.Id == gameId, cancellationToken))
        {
            throw new NotFoundException($"Game {gameId} was not found.");
        }

        if (!await _achievements.AnyAsync(achievement => achievement.Id == achievementId, cancellationToken))
        {
            throw new NotFoundException($"Achievement {achievementId} was not found.");
        }
    }

    private async Task EnsureUniquePairAsync(int userId, int achievementId, int? excludeId, CancellationToken cancellationToken)
    {
        var exists = await _usersAchievements.AnyAsync(
            item => item.UserId == userId && item.AchievementId == achievementId && (!excludeId.HasValue || item.Id != excludeId.Value),
            cancellationToken);

        if (exists)
        {
            throw new ConflictException("This user already has this achievement.");
        }
    }

    private async Task<UsersAchievement> GetRequiredAsync(int id, CancellationToken cancellationToken)
    {
        return await _usersAchievements.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Users-achievement {id} was not found.");
    }

    private static UsersAchievementDto Map(UsersAchievement item) => new()
    {
        Id = item.Id,
        AchievementId = item.AchievementId,
        UserId = item.UserId,
        GameId = item.GameId,
        UnlockDate = item.UnlockDate
    };
}
