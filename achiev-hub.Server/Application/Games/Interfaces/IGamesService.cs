using achiev_hub.Server.Application.Achievements;
using achiev_hub.Server.Application.Common;
using achiev_hub.Server.Application.Games;

namespace achiev_hub.Server.Application.Games.Interfaces;
public interface IGamesService
{
    Task<PagedResultDto<RecentGameDto>> GetRecentGamesAsync(
        string steamId,
        int page,
        int pageSize,
        int? userId = null,
        CancellationToken cancellationToken = default);

    Task<PagedResultDto<LibraryGameDto>> GetLibraryAsync(
        string steamId,
        int page,
        int pageSize,
        int? userId = null,
        LibraryGameFilterDto? filters = null,
        CancellationToken cancellationToken = default);

    Task<GameDetailsDto?> GetGameDetailsAsync(int appId, CancellationToken cancellationToken = default);

    Task<PagedResultDto<AchievementDto>> GetAchievementsAsync(
        string steamId,
        int appId,
        int page,
        int pageSize,
        string? name = null,
        string? status = null,
        string? sort = null,
        string? global = null,
        int? userId = null,
        CancellationToken cancellationToken = default);

    Task<PagedResultDto<RecentAchievementDto>> GetRecentAchievementsAsync(
        int userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
