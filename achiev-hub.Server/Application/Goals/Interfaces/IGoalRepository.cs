using achiev_hub.Server.Application.Common;
using achiev_hub.Server.Domain.Entities;
using achiev_hub.Server.Domain.Enums;

namespace achiev_hub.Server.Application.Goals.Interfaces;

public interface IGoalRepository
{
    Task<PagedResultDto<Goal>> GetPageForUserAsync(
        int userId,
        GoalStatus? status,
        string? gameSteamId,
        decimal? minEndGoal,
        decimal? minCompletion,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GoalFilterGameRow>> GetFilterGamesAsync(
        int userId,
        GoalStatus? status,
        CancellationToken cancellationToken = default);

    Task<Goal?> GetByIdForUserAsync(
        int userId,
        int goalId,
        bool trackChanges,
        CancellationToken cancellationToken = default);

    Task<Goal?> GetByIdWithAchievementDetailsAsync(
        int userId,
        int goalId,
        CancellationToken cancellationToken = default);

    Task<Goal?> GetActiveBySteamAppIdAsync(
        int userId,
        string gameSteamId,
        CancellationToken cancellationToken = default);

    Task<Goal?> GetActiveForUpdateAsync(
        int userId,
        int gameId,
        CancellationToken cancellationToken = default);

    Task<bool> HasActiveAsync(
        int userId,
        int gameId,
        int? excludeGoalId = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<int>> GetActiveGameIdsAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DateTimeOffset>> GetCompletedDatesSinceAsync(
        int userId,
        DateTimeOffset since,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Goal>> GetCompletedAfterAsync(
        int userId,
        DateTimeOffset? acknowledgedAt,
        CancellationToken cancellationToken = default);

    void Add(Goal goal);

    void Remove(Goal goal);

    void RemoveGoalAchievement(GoalAchievement goalAchievement);
}

public sealed class GoalFilterGameRow
{
    public string GameSteamId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
