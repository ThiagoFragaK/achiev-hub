using achiev_hub.Server.Application.Common;
using achiev_hub.Server.Domain.Entities;

namespace achiev_hub.Server.Application.Users.Interfaces;

public interface IUsersGameRepository
{
    Task<IReadOnlyList<UsersGameAchievementStatsRow>> GetGamesWithAchievementStatsAsync(
        int userId,
        bool excludeCompleted = false,
        string? nameQuery = null,
        IReadOnlyCollection<int>? excludeGameIds = null,
        int? take = null,
        CancellationToken cancellationToken = default);

    Task<(decimal Average, int GamesInAverage)> GetAveragePercentageStatsAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task<decimal?> GetAchievementPercentageAsync(
        int userId,
        int gameId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<int, decimal>> GetAchievementPercentagesAsync(
        int userId,
        IReadOnlyCollection<int> gameIds,
        CancellationToken cancellationToken = default);

    Task<int> CountCommunityVisibleGamesAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<decimal>> GetCommunityVisiblePercentagesAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PercentageBandCountRow>> GetPercentageBandCountsAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task<PagedResultDto<LibraryGameRow>> GetLibraryPageAsync(
        int userId,
        LibraryGameQuery query,
        CancellationToken cancellationToken = default);

    Task<UsersGame?> GetTrackedByUserAndGameAsync(
        int userId,
        int gameId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UsersGame>> GetTrackedByUserAndGameIdsAsync(
        int userId,
        IReadOnlyCollection<int> gameIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetOwnedWithStatsSteamIdsAsync(
        int userId,
        CancellationToken cancellationToken = default);

    void Add(UsersGame usersGame);
}

public sealed class UsersGameAchievementStatsRow
{
    public int GameId { get; set; }
    public string GameSteamId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public decimal AchievementsPercentage { get; set; }
}

public sealed class LibraryGameQuery
{
    public string? Name { get; set; }
    public double? MinHours { get; set; }
    public bool? HasAchievements { get; set; }
    public string? Sort { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}

public sealed class LibraryGameRow
{
    public string GameSteamId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public int PlaytimeMinutes { get; set; }
    public long? LastPlayedUnix { get; set; }
    public bool HasAchievements { get; set; }
    public decimal AchievementsPercentage { get; set; }
}

public sealed class PercentageBandCountRow
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
}
