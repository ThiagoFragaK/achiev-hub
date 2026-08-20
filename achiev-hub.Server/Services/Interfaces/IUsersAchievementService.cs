using achiev_hub.Server.DTOs.Persistence;

namespace achiev_hub.Server.Services.Interfaces;

public interface IUsersAchievementService
{
    Task<IReadOnlyList<UsersAchievementDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<UsersAchievementDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<UsersAchievementDto> CreateAsync(CreateUsersAchievementRequest request, CancellationToken cancellationToken = default);
    Task<UsersAchievementDto> UpdateAsync(int id, UpdateUsersAchievementRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
