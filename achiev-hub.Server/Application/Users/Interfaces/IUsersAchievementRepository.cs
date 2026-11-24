using achiev_hub.Server.Application.Common;
using achiev_hub.Server.Domain.Entities;

namespace achiev_hub.Server.Application.Users.Interfaces;

public interface IUsersAchievementRepository
{
    Task<IReadOnlyList<UnlockDayCountRow>> GetDailyUnlockCountsAsync(
        int userId,
        DateTime sinceUtc,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UnlockYearCountRow>> GetYearlyUnlockCountsAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UnlockMonthCountRow>> GetMonthlyUnlockCountsAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task<int> CountForUserAsync(int userId, CancellationToken cancellationToken = default);

    Task<int> CountForGameAsync(int userId, int gameId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DateTime?>> GetUnlockDatesListAsync(
        int userId,
        int gameId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<int, DateTime?>> GetUnlockDatesByAchievementIdAsync(
        int userId,
        int gameId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlySet<int>> GetUnlockedAchievementIdsAsync(
        int userId,
        int gameId,
        IReadOnlyCollection<int> achievementIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<int, int>> GetCountsByGameIdsAsync(
        int userId,
        IReadOnlyCollection<int> gameIds,
        CancellationToken cancellationToken = default);

    Task<PagedResultDto<RecentAchievementRow>> GetRecentUnlockedPageAsync(
        int userId,
        int page,
        int pageSize,
        int maximumRows,
        CancellationToken cancellationToken = default);

    Task<PagedResultDto<RecentAchievementRow>> GetUnlockedPageForMonthAsync(
        int userId,
        int year,
        int month,
        int page,
        int pageSize,
        string? nameQuery = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UsersAchievement>> GetTrackedByUserAndGameAsync(
        int userId,
        int gameId,
        CancellationToken cancellationToken = default);

    void Add(UsersAchievement usersAchievement);

    void Remove(UsersAchievement usersAchievement);
}

public sealed class UnlockDayCountRow
{
    public DateTime Day { get; set; }
    public int Count { get; set; }
}

public sealed class UnlockYearCountRow
{
    public int Year { get; set; }
    public int Count { get; set; }
}

public sealed class UnlockMonthCountRow
{
    public int Year { get; set; }
    public int Month { get; set; }
    public int Count { get; set; }
}

public sealed class RecentAchievementRow
{
    public string AchievementName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrlUnlock { get; set; }
    public decimal? GlobalPercentage { get; set; }
    public DateTime? UnlockDate { get; set; }
    public string GameName { get; set; } = string.Empty;
    public string GameSteamId { get; set; } = string.Empty;
}
