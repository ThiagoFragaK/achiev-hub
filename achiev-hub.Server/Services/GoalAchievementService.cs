using achiev_hub.Server.DTOs.Persistence;
using achiev_hub.Server.Entities;
using achiev_hub.Server.Exceptions;
using achiev_hub.Server.Repositories.Interfaces;
using achiev_hub.Server.Services.Interfaces;

namespace achiev_hub.Server.Services;

public class GoalAchievementService : IGoalAchievementService
{
    private readonly IRepository<GoalAchievement> _goalAchievements;
    private readonly IRepository<Goal> _goals;
    private readonly IRepository<Achievement> _achievements;

    public GoalAchievementService(
        IRepository<GoalAchievement> goalAchievements,
        IRepository<Goal> goals,
        IRepository<Achievement> achievements)
    {
        _goalAchievements = goalAchievements;
        _goals = goals;
        _achievements = achievements;
    }

    public async Task<IReadOnlyList<GoalAchievementDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var items = await _goalAchievements.GetAllAsync(cancellationToken);
        return items.Select(Map).ToList();
    }

    public async Task<GoalAchievementDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return Map(await GetRequiredAsync(id, cancellationToken));
    }

    public async Task<GoalAchievementDto> CreateAsync(CreateGoalAchievementRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureForeignKeysAsync(request.GoalId, request.AchievementId, cancellationToken);
        await EnsureUniquePairAsync(request.GoalId, request.AchievementId, null, cancellationToken);

        var item = new GoalAchievement
        {
            AchievementId = request.AchievementId,
            GoalId = request.GoalId,
            Status = request.Status
        };

        await _goalAchievements.AddAsync(item, cancellationToken);
        await _goalAchievements.SaveChangesAsync(cancellationToken);
        return Map(item);
    }

    public async Task<GoalAchievementDto> UpdateAsync(int id, UpdateGoalAchievementRequest request, CancellationToken cancellationToken = default)
    {
        var item = await GetRequiredAsync(id, cancellationToken);
        await EnsureForeignKeysAsync(request.GoalId, request.AchievementId, cancellationToken);
        await EnsureUniquePairAsync(request.GoalId, request.AchievementId, id, cancellationToken);

        item.AchievementId = request.AchievementId;
        item.GoalId = request.GoalId;
        item.Status = request.Status;

        _goalAchievements.Update(item);
        await _goalAchievements.SaveChangesAsync(cancellationToken);
        return Map(item);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var item = await GetRequiredAsync(id, cancellationToken);
        _goalAchievements.Remove(item);
        await _goalAchievements.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureForeignKeysAsync(int goalId, int achievementId, CancellationToken cancellationToken)
    {
        if (!await _goals.AnyAsync(goal => goal.Id == goalId, cancellationToken))
        {
            throw new NotFoundException($"Goal {goalId} was not found.");
        }

        if (!await _achievements.AnyAsync(achievement => achievement.Id == achievementId, cancellationToken))
        {
            throw new NotFoundException($"Achievement {achievementId} was not found.");
        }
    }

    private async Task EnsureUniquePairAsync(int goalId, int achievementId, int? excludeId, CancellationToken cancellationToken)
    {
        var exists = await _goalAchievements.AnyAsync(
            item => item.GoalId == goalId && item.AchievementId == achievementId && (!excludeId.HasValue || item.Id != excludeId.Value),
            cancellationToken);

        if (exists)
        {
            throw new ConflictException("This goal already includes this achievement.");
        }
    }

    private async Task<GoalAchievement> GetRequiredAsync(int id, CancellationToken cancellationToken)
    {
        return await _goalAchievements.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Goal-achievement {id} was not found.");
    }

    private static GoalAchievementDto Map(GoalAchievement item) => new()
    {
        Id = item.Id,
        AchievementId = item.AchievementId,
        GoalId = item.GoalId,
        Status = item.Status
    };
}
