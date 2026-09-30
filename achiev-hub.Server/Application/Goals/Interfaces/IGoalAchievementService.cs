using achiev_hub.Server.Application.Users;
using achiev_hub.Server.Application.Users.Interfaces;
using achiev_hub.Server.Application.Games;
using achiev_hub.Server.Application.Games.Interfaces;
using achiev_hub.Server.Application.Achievements;
using achiev_hub.Server.Application.Achievements.Interfaces;
using achiev_hub.Server.Application.Goals;
using achiev_hub.Server.Application.Goals.Interfaces;

namespace achiev_hub.Server.Application.Goals.Interfaces;
public interface IGoalAchievementService
{
    Task<IReadOnlyList<GoalAchievementDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<GoalAchievementDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<GoalAchievementDto> CreateAsync(CreateGoalAchievementRequest request, CancellationToken cancellationToken = default);
    Task<GoalAchievementDto> UpdateAsync(int id, UpdateGoalAchievementRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
