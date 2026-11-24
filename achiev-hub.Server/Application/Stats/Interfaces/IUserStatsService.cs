using achiev_hub.Server.Application.Common;
using achiev_hub.Server.Application.Games;
using achiev_hub.Server.Application.Stats;

namespace achiev_hub.Server.Application.Stats.Interfaces;
public interface IUserStatsService
{
    Task<UserStatsDto> GetStatsAsync(int userId, CancellationToken cancellationToken = default);

    UserStatsDto GetEmptyStats();

    Task<GameProgressDto> GetGameProgressAsync(int userId, int appId, CancellationToken cancellationToken = default);

    GameProgressDto GetEmptyGameProgress();

    Task<AchievementsByMonthDto> GetAchievementsByMonthAsync(int userId, CancellationToken cancellationToken = default);

    AchievementsByMonthDto GetEmptyAchievementsByMonth();

    Task<PagedResultDto<RecentAchievementDto>> GetAchievementsForMonthAsync(
        int userId,
        int year,
        int month,
        int page,
        int pageSize,
        string? nameQuery = null,
        CancellationToken cancellationToken = default);

    Task<GamesPercentageBandsDto> GetGamesPercentageBandsAsync(int userId, CancellationToken cancellationToken = default);

    GamesPercentageBandsDto GetEmptyGamesPercentageBands();

    Task<GamesPercentageBandGamesDto> GetGamesPercentageBandGamesAsync(int userId, CancellationToken cancellationToken = default);

    GamesPercentageBandGamesDto GetEmptyGamesPercentageBandGames();

    Task<GamesForAverageDto> GetGamesForAverageAsync(int userId, CancellationToken cancellationToken = default);

    GamesForAverageDto GetEmptyGamesForAverage();

    Task<CompletedGamesByYearDto> GetCompletedGamesByYearAsync(int userId, CancellationToken cancellationToken = default);

    CompletedGamesByYearDto GetEmptyCompletedGamesByYear();
}
