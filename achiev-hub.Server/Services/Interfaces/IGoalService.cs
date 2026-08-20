using achiev_hub.Server.DTOs.Persistence;

namespace achiev_hub.Server.Services.Interfaces;

public interface IGoalService
{
    Task<IReadOnlyList<GoalDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<GoalDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<GoalDto> CreateAsync(CreateGoalRequest request, CancellationToken cancellationToken = default);
    Task<GoalDto> UpdateAsync(int id, UpdateGoalRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
