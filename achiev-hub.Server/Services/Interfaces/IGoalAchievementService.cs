using achiev_hub.Server.DTOs.Persistence;

namespace achiev_hub.Server.Services.Interfaces;

public interface IGoalAchievementService
{
    Task<IReadOnlyList<GoalAchievementDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<GoalAchievementDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<GoalAchievementDto> CreateAsync(CreateGoalAchievementRequest request, CancellationToken cancellationToken = default);
    Task<GoalAchievementDto> UpdateAsync(int id, UpdateGoalAchievementRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
