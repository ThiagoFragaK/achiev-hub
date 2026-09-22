using achiev_hub.Server.DTOs;

namespace achiev_hub.Server.Services.Interfaces;

public interface IUserStatsService
{
    Task<UserStatsDto> GetStatsAsync(int userId, CancellationToken cancellationToken = default);

    UserStatsDto GetEmptyStats();

    Task<GameProgressDto> GetGameProgressAsync(int userId, int appId, CancellationToken cancellationToken = default);

    GameProgressDto GetEmptyGameProgress();
}
