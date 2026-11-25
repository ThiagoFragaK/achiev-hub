using achiev_hub.Server.Application.Common;
using achiev_hub.Server.Application.Users.Interfaces;
using achiev_hub.Server.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace achiev_hub.Server.Infrastructure.Persistence.Repository;

public class UsersAchievementRepository : IUsersAchievementRepository
{
    private readonly ApplicationDbContext _db;

    public UsersAchievementRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<UnlockDayCountRow>> GetDailyUnlockCountsAsync(
        int userId,
        DateTime sinceUtc,
        CancellationToken cancellationToken = default)
    {
        var rows = await _db.UsersAchievements
            .AsNoTracking()
            .Where(ua => ua.UserId == userId && ua.UnlockDate != null && ua.UnlockDate >= sinceUtc)
            .GroupBy(ua => ua.UnlockDate!.Value.Date)
            .Select(g => new UnlockDayCountRow { Day = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return rows;
    }

    public async Task<IReadOnlyList<UnlockYearCountRow>> GetYearlyUnlockCountsAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        return await _db.UsersAchievements
            .AsNoTracking()
            .Where(ua => ua.UserId == userId && ua.UnlockDate != null)
            .GroupBy(ua => ua.UnlockDate!.Value.Year)
            .Select(g => new UnlockYearCountRow { Year = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UnlockMonthCountRow>> GetMonthlyUnlockCountsAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        return await _db.UsersAchievements
            .AsNoTracking()
            .Where(ua => ua.UserId == userId && ua.UnlockDate != null)
            .GroupBy(ua => new { ua.UnlockDate!.Value.Year, ua.UnlockDate!.Value.Month })
            .Select(g => new UnlockMonthCountRow
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                Count = g.Count()
            })
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountForUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        return _db.UsersAchievements
            .AsNoTracking()
            .CountAsync(ua => ua.UserId == userId, cancellationToken);
    }

    public Task<int> CountForGameAsync(int userId, int gameId, CancellationToken cancellationToken = default)
    {
        return _db.UsersAchievements
            .AsNoTracking()
            .CountAsync(ua => ua.UserId == userId && ua.GameId == gameId, cancellationToken);
    }

    public async Task<IReadOnlyList<DateTime?>> GetUnlockDatesListAsync(
        int userId,
        int gameId,
        CancellationToken cancellationToken = default)
    {
        return await _db.UsersAchievements
            .AsNoTracking()
            .Where(ua => ua.UserId == userId && ua.GameId == gameId)
            .Select(ua => ua.UnlockDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<int, DateTime?>> GetUnlockDatesByAchievementIdAsync(
        int userId,
        int gameId,
        CancellationToken cancellationToken = default)
    {
        return await _db.UsersAchievements
            .AsNoTracking()
            .Where(ua => ua.UserId == userId && ua.GameId == gameId)
            .ToDictionaryAsync(ua => ua.AchievementId, ua => ua.UnlockDate, cancellationToken);
    }

    public async Task<IReadOnlySet<int>> GetUnlockedAchievementIdsAsync(
        int userId,
        int gameId,
        IReadOnlyCollection<int> achievementIds,
        CancellationToken cancellationToken = default)
    {
        if (achievementIds.Count == 0)
        {
            return new HashSet<int>();
        }

        var ids = await _db.UsersAchievements
            .AsNoTracking()
            .Where(ua =>
                ua.UserId == userId
                && ua.GameId == gameId
                && achievementIds.Contains(ua.AchievementId))
            .Select(ua => ua.AchievementId)
            .ToListAsync(cancellationToken);

        return ids.ToHashSet();
    }

    public async Task<IReadOnlyDictionary<int, int>> GetCountsByGameIdsAsync(
        int userId,
        IReadOnlyCollection<int> gameIds,
        CancellationToken cancellationToken = default)
    {
        if (gameIds.Count == 0)
        {
            return new Dictionary<int, int>();
        }

        return await _db.UsersAchievements
            .AsNoTracking()
            .Where(ua => ua.UserId == userId && gameIds.Contains(ua.GameId))
            .GroupBy(ua => ua.GameId)
            .Select(g => new { GameId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.GameId, x => x.Count, cancellationToken);
    }

    public async Task<PagedResultDto<RecentAchievementRow>> GetRecentUnlockedPageAsync(
        int userId,
        int page,
        int pageSize,
        int maximumRows,
        CancellationToken cancellationToken = default)
    {
        var unlockQuery = _db.UsersAchievements
            .AsNoTracking()
            .Where(ua => ua.UserId == userId && ua.UnlockDate != null);

        var unlockedCount = await unlockQuery.CountAsync(cancellationToken);
        var totalCount = Math.Min(unlockedCount, maximumRows);
        var size = pageSize <= 0 ? 10 : Math.Min(pageSize, 10);
        var pagination = PaginationDto.Create(page, size, totalCount, defaultPageSize: 10);

        if (totalCount == 0)
        {
            return PagedResultDto<RecentAchievementRow>.From([], pagination);
        }

        var take = Math.Min(pagination.PerPage, totalCount - pagination.Skip);
        if (take <= 0)
        {
            return PagedResultDto<RecentAchievementRow>.From([], pagination);
        }

        var rows = await unlockQuery
            .OrderByDescending(ua => ua.UnlockDate)
            .Skip(pagination.Skip)
            .Take(take)
            .Select(ua => new RecentAchievementRow
            {
                AchievementName = ua.Achievement.Name,
                Description = ua.Achievement.Description,
                ImageUrlUnlock = ua.Achievement.ImageUrlUnlock,
                GlobalPercentage = ua.Achievement.GlobalPercentage,
                UnlockDate = ua.UnlockDate,
                GameName = ua.Game.Name,
                GameSteamId = ua.Game.GameSteamId
            })
            .ToListAsync(cancellationToken);

        return PagedResultDto<RecentAchievementRow>.From(rows, pagination);
    }

    public async Task<PagedResultDto<RecentAchievementRow>> GetUnlockedPageForMonthAsync(
        int userId,
        int year,
        int month,
        int page,
        int pageSize,
        string? nameQuery = null,
        CancellationToken cancellationToken = default)
    {
        var unlockQuery = _db.UsersAchievements
            .AsNoTracking()
            .Where(ua =>
                ua.UserId == userId
                && ua.UnlockDate != null
                && ua.UnlockDate.Value.Year == year
                && ua.UnlockDate.Value.Month == month);

        if (!string.IsNullOrWhiteSpace(nameQuery))
        {
            var pattern = $"%{nameQuery.Trim()}%";
            unlockQuery = unlockQuery.Where(ua => EF.Functions.ILike(ua.Achievement.Name, pattern));
        }

        var totalCount = await unlockQuery.CountAsync(cancellationToken);
        var pagination = PaginationDto.Create(page, pageSize, totalCount, defaultPageSize: 10);

        if (totalCount == 0)
        {
            return PagedResultDto<RecentAchievementRow>.From([], pagination);
        }

        var take = Math.Min(pagination.PerPage, totalCount - pagination.Skip);
        if (take <= 0)
        {
            return PagedResultDto<RecentAchievementRow>.From([], pagination);
        }

        var rows = await unlockQuery
            .OrderByDescending(ua => ua.UnlockDate)
            .Skip(pagination.Skip)
            .Take(take)
            .Select(ua => new RecentAchievementRow
            {
                AchievementName = ua.Achievement.Name,
                Description = ua.Achievement.Description,
                ImageUrlUnlock = ua.Achievement.ImageUrlUnlock,
                GlobalPercentage = ua.Achievement.GlobalPercentage,
                UnlockDate = ua.UnlockDate,
                GameName = ua.Game.Name,
                GameSteamId = ua.Game.GameSteamId
            })
            .ToListAsync(cancellationToken);

        return PagedResultDto<RecentAchievementRow>.From(rows, pagination);
    }

    public async Task<IReadOnlyList<UsersAchievement>> GetTrackedByUserAndGameAsync(
        int userId,
        int gameId,
        CancellationToken cancellationToken = default)
    {
        return await _db.UsersAchievements
            .Where(ua => ua.UserId == userId && ua.GameId == gameId)
            .ToListAsync(cancellationToken);
    }

    public void Add(UsersAchievement usersAchievement)
    {
        _db.UsersAchievements.Add(usersAchievement);
    }

    public void Remove(UsersAchievement usersAchievement)
    {
        _db.UsersAchievements.Remove(usersAchievement);
    }
}
