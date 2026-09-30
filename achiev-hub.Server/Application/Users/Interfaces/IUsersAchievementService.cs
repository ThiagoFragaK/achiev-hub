using achiev_hub.Server.Application.Users;
using achiev_hub.Server.Application.Users.Interfaces;
using achiev_hub.Server.Application.Games;
using achiev_hub.Server.Application.Games.Interfaces;
using achiev_hub.Server.Application.Achievements;
using achiev_hub.Server.Application.Achievements.Interfaces;
using achiev_hub.Server.Application.Goals;
using achiev_hub.Server.Application.Goals.Interfaces;

namespace achiev_hub.Server.Application.Users.Interfaces;
public interface IUsersAchievementService
{
    Task<IReadOnlyList<UsersAchievementDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<UsersAchievementDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<UsersAchievementDto> CreateAsync(CreateUsersAchievementRequest request, CancellationToken cancellationToken = default);
    Task<UsersAchievementDto> UpdateAsync(int id, UpdateUsersAchievementRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
