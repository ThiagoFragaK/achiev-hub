using achiev_hub.Server.Domain.Enums;

namespace achiev_hub.Server.Application.Goals.Interfaces;

public interface IGoalService
{
    Task<IReadOnlyList<GoalDto>> GetForUserAsync(int userId, GoalStatus? status = null, CancellationToken cancellationToken = default);
    Task<GoalDto> GetByIdAsync(int userId, int id, CancellationToken cancellationToken = default);
    Task<GoalDto?> GetActiveByAppIdAsync(int userId, int appId, CancellationToken cancellationToken = default);
    Task<GoalDto> CreateAsync(int userId, CreateGoalRequest request, CancellationToken cancellationToken = default);
    Task<GoalDto> UpdateAsync(int userId, int id, UpdateGoalRequest request, CancellationToken cancellationToken = default);
    Task<GoalDto> AttachAchievementsAsync(int userId, int id, AttachGoalAchievementsRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GoalAchievementDetailDto>> GetAchievementsAsync(int userId, int id, CancellationToken cancellationToken = default);
    Task<GoalDto> ReplaceAchievementsAsync(int userId, int id, ReplaceGoalAchievementsRequest request, CancellationToken cancellationToken = default);
    Task<GoalDto> AbandonAsync(int userId, int id, CancellationToken cancellationToken = default);
    Task DeleteAsync(int userId, int id, CancellationToken cancellationToken = default);
    Task RefreshForGameAsync(int userId, int gameId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EligibleGameDto>> GetEligibleGamesAsync(int userId, string? query = null, CancellationToken cancellationToken = default);
    Task<GoalsCompletedSeriesDto> GetCompletedSeriesAsync(int userId, CancellationToken cancellationToken = default);
}
