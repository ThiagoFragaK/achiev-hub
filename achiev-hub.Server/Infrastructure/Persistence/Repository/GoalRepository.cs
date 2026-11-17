using achiev_hub.Server.Application.Common;
using achiev_hub.Server.Application.Goals.Interfaces;
using achiev_hub.Server.Domain.Entities;
using achiev_hub.Server.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace achiev_hub.Server.Infrastructure.Persistence.Repository;

public class GoalRepository : IGoalRepository
{
    private readonly ApplicationDbContext _db;

    public GoalRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResultDto<Goal>> GetPageForUserAsync(
        int userId,
        GoalStatus? status,
        string? gameSteamId,
        decimal? minEndGoal,
        decimal? minCompletion,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Goals
            .AsNoTracking()
            .Include(g => g.Game)
            .Include(g => g.GoalAchievements)
            .Where(g => g.UserId == userId);

        if (status is GoalStatus filterStatus)
        {
            query = query.Where(g => g.Status == filterStatus);
        }

        if (!string.IsNullOrWhiteSpace(gameSteamId))
        {
            query = query.Where(g => g.Game.GameSteamId == gameSteamId);
        }

        if (minEndGoal is decimal endGoal)
        {
            query = query.Where(g => g.PercentageGoal >= endGoal);
        }

        if (minCompletion is decimal completion && completion > 0)
        {
            query = query.Where(g =>
                g.GoalAchievements.Count > 0
                && (g.GoalAchievements.Count - g.AchievementsMissing) * 100m / g.GoalAchievements.Count
                    >= completion);
        }

        query = query.OrderByDescending(g => g.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);
        var pagination = PaginationDto.Create(page, pageSize, totalCount, defaultPageSize: 15);

        var items = await query
            .Skip(pagination.Skip)
            .Take(pagination.PerPage)
            .ToListAsync(cancellationToken);

        return PagedResultDto<Goal>.From(items, pagination);
    }

    public async Task<IReadOnlyList<GoalFilterGameRow>> GetFilterGamesAsync(
        int userId,
        GoalStatus? status,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Goals
            .AsNoTracking()
            .Where(g => g.UserId == userId);

        if (status.HasValue)
        {
            query = query.Where(g => g.Status == status.Value);
        }

        return await query
            .Select(g => new GoalFilterGameRow
            {
                GameSteamId = g.Game.GameSteamId,
                Name = g.Game.Name
            })
            .Distinct()
            .OrderBy(g => g.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Goal?> GetByIdForUserAsync(
        int userId,
        int goalId,
        bool trackChanges,
        CancellationToken cancellationToken = default)
    {
        var query = trackChanges
            ? _db.Goals.Include(g => g.Game).Include(g => g.GoalAchievements)
            : _db.Goals.AsNoTracking().Include(g => g.Game).Include(g => g.GoalAchievements);

        return await query.FirstOrDefaultAsync(g => g.Id == goalId && g.UserId == userId, cancellationToken);
    }

    public Task<Goal?> GetByIdWithAchievementDetailsAsync(
        int userId,
        int goalId,
        CancellationToken cancellationToken = default)
    {
        return _db.Goals
            .AsNoTracking()
            .Include(g => g.Game)
            .Include(g => g.GoalAchievements)
            .ThenInclude(ga => ga.Achievement)
            .FirstOrDefaultAsync(g => g.Id == goalId && g.UserId == userId, cancellationToken);
    }

    public Task<Goal?> GetActiveBySteamAppIdAsync(
        int userId,
        string gameSteamId,
        CancellationToken cancellationToken = default)
    {
        return _db.Goals
            .AsNoTracking()
            .Include(g => g.Game)
            .Include(g => g.GoalAchievements)
            .FirstOrDefaultAsync(
                g => g.UserId == userId
                     && g.Status == GoalStatus.Active
                     && g.Game.GameSteamId == gameSteamId,
                cancellationToken);
    }

    public Task<Goal?> GetActiveForUpdateAsync(
        int userId,
        int gameId,
        CancellationToken cancellationToken = default)
    {
        return _db.Goals
            .Include(g => g.GoalAchievements)
            .FirstOrDefaultAsync(
                g => g.UserId == userId && g.GameId == gameId && g.Status == GoalStatus.Active,
                cancellationToken);
    }

    public Task<bool> HasActiveAsync(
        int userId,
        int gameId,
        int? excludeGoalId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Goals.Where(g =>
            g.UserId == userId
            && g.GameId == gameId
            && g.Status == GoalStatus.Active);

        if (excludeGoalId is int excludeId)
        {
            query = query.Where(g => g.Id != excludeId);
        }

        return query.AnyAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<int>> GetActiveGameIdsAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        return await _db.Goals
            .AsNoTracking()
            .Where(g => g.UserId == userId && g.Status == GoalStatus.Active)
            .Select(g => g.GameId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DateTimeOffset>> GetCompletedDatesSinceAsync(
        int userId,
        DateTimeOffset since,
        CancellationToken cancellationToken = default)
    {
        return await _db.Goals
            .AsNoTracking()
            .Where(g =>
                g.UserId == userId
                && g.Status == GoalStatus.Completed
                && g.CompletedAt != null
                && g.CompletedAt >= since)
            .Select(g => g.CompletedAt!.Value)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Goal>> GetCompletedAfterAsync(
        int userId,
        DateTimeOffset? acknowledgedAt,
        CancellationToken cancellationToken = default)
    {
        const int maxUnacknowledged = 50;

        return await _db.Goals
            .AsNoTracking()
            .Include(g => g.Game)
            .Include(g => g.GoalAchievements)
            .Where(g =>
                g.UserId == userId
                && g.Status == GoalStatus.Completed
                && g.CompletedAt != null
                && (acknowledgedAt == null || g.CompletedAt > acknowledgedAt))
            .OrderByDescending(g => g.CompletedAt)
            .Take(maxUnacknowledged)
            .ToListAsync(cancellationToken);
    }

    public void Add(Goal goal)
    {
        _db.Goals.Add(goal);
    }

    public void Remove(Goal goal)
    {
        _db.Goals.Remove(goal);
    }

    public void RemoveGoalAchievement(GoalAchievement goalAchievement)
    {
        _db.GoalAchievements.Remove(goalAchievement);
    }
}
