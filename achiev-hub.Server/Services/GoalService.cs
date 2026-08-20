using achiev_hub.Server.DTOs.Persistence;
using achiev_hub.Server.Entities;
using achiev_hub.Server.Exceptions;
using achiev_hub.Server.Repositories.Interfaces;
using achiev_hub.Server.Services.Interfaces;

namespace achiev_hub.Server.Services;

public class GoalService : IGoalService
{
    private readonly IRepository<Goal> _goals;
    private readonly IRepository<Game> _games;
    private readonly IRepository<User> _users;

    public GoalService(IRepository<Goal> goals, IRepository<Game> games, IRepository<User> users)
    {
        _goals = goals;
        _games = games;
        _users = users;
    }

    public async Task<IReadOnlyList<GoalDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var goals = await _goals.GetAllAsync(cancellationToken);
        return goals.Select(Map).ToList();
    }

    public async Task<GoalDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return Map(await GetRequiredAsync(id, cancellationToken));
    }

    public async Task<GoalDto> CreateAsync(CreateGoalRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureForeignKeysAsync(request.UserId, request.GameId, cancellationToken);

        var goal = new Goal
        {
            GameId = request.GameId,
            UserId = request.UserId,
            PercentageGoal = request.PercentageGoal,
            AchievementsMissing = request.AchievementsMissing
        };

        await _goals.AddAsync(goal, cancellationToken);
        await _goals.SaveChangesAsync(cancellationToken);
        return Map(goal);
    }

    public async Task<GoalDto> UpdateAsync(int id, UpdateGoalRequest request, CancellationToken cancellationToken = default)
    {
        var goal = await GetRequiredAsync(id, cancellationToken);
        await EnsureForeignKeysAsync(request.UserId, request.GameId, cancellationToken);

        goal.GameId = request.GameId;
        goal.UserId = request.UserId;
        goal.PercentageGoal = request.PercentageGoal;
        goal.AchievementsMissing = request.AchievementsMissing;

        _goals.Update(goal);
        await _goals.SaveChangesAsync(cancellationToken);
        return Map(goal);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var goal = await GetRequiredAsync(id, cancellationToken);
        _goals.Remove(goal);
        await _goals.SaveChangesAsync(cancellationToken);
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

    private async Task<Goal> GetRequiredAsync(int id, CancellationToken cancellationToken)
    {
        return await _goals.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Goal {id} was not found.");
    }

    private static GoalDto Map(Goal goal) => new()
    {
        Id = goal.Id,
        GameId = goal.GameId,
        UserId = goal.UserId,
        PercentageGoal = goal.PercentageGoal,
        AchievementsMissing = goal.AchievementsMissing
    };
}
