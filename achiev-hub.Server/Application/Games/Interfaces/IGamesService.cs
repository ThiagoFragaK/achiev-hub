using achiev_hub.Server.Application.Games;
using achiev_hub.Server.Application.Games.Interfaces;
using achiev_hub.Server.Application.Achievements;
using achiev_hub.Server.Application.Achievements.Interfaces;
using achiev_hub.Server.Application.Steam;
using achiev_hub.Server.Application.Steam.Interfaces;
using achiev_hub.Server.Application.Stats;
using achiev_hub.Server.Application.Stats.Interfaces;
using achiev_hub.Server.Application.Users;
using achiev_hub.Server.Application.Users.Interfaces;
using achiev_hub.Server.Application.Goals;
using achiev_hub.Server.Application.Goals.Interfaces;
using achiev_hub.Server.Application.Auth;
using achiev_hub.Server.Application.Auth.Interfaces;

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
        int? userId = null,
        CancellationToken cancellationToken = default);
}
