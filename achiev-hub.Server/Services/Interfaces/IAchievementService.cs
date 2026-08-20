using achiev_hub.Server.DTOs.Persistence;

namespace achiev_hub.Server.Services.Interfaces;

public interface IAchievementService
{
    Task<IReadOnlyList<AchievementRecordDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<AchievementRecordDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<AchievementRecordDto> CreateAsync(CreateAchievementRequest request, CancellationToken cancellationToken = default);
    Task<AchievementRecordDto> UpdateAsync(int id, UpdateAchievementRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
