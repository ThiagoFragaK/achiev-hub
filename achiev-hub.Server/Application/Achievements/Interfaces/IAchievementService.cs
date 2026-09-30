using achiev_hub.Server.Application.Users;
using achiev_hub.Server.Application.Users.Interfaces;
using achiev_hub.Server.Application.Games;
using achiev_hub.Server.Application.Games.Interfaces;
using achiev_hub.Server.Application.Achievements;
using achiev_hub.Server.Application.Achievements.Interfaces;
using achiev_hub.Server.Application.Goals;
using achiev_hub.Server.Application.Goals.Interfaces;

namespace achiev_hub.Server.Application.Achievements.Interfaces;
public interface IAchievementService
{
    Task<IReadOnlyList<AchievementRecordDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<AchievementRecordDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<AchievementRecordDto> CreateAsync(CreateAchievementRequest request, CancellationToken cancellationToken = default);
    Task<AchievementRecordDto> UpdateAsync(int id, UpdateAchievementRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
