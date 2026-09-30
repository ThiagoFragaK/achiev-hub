using achiev_hub.Server.Application.Users;
using achiev_hub.Server.Application.Users.Interfaces;
using achiev_hub.Server.Application.Games;
using achiev_hub.Server.Application.Games.Interfaces;
using achiev_hub.Server.Application.Achievements;
using achiev_hub.Server.Application.Achievements.Interfaces;
using achiev_hub.Server.Application.Goals;
using achiev_hub.Server.Application.Goals.Interfaces;

namespace achiev_hub.Server.Application.Goals.Interfaces;
public interface IGoalService
{
    Task<IReadOnlyList<GoalDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<GoalDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<GoalDto> CreateAsync(CreateGoalRequest request, CancellationToken cancellationToken = default);
    Task<GoalDto> UpdateAsync(int id, UpdateGoalRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
