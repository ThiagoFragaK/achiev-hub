using achiev_hub.Server.Application.Achievements.Interfaces;
using achiev_hub.Server.Application.Common;
using achiev_hub.Server.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace achiev_hub.Server.Infrastructure.Persistence.Repository;

public class AchievementRepository : IAchievementRepository
{
    private readonly ApplicationDbContext _db;

    public AchievementRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<int> CountForGameAsync(int gameId, CancellationToken cancellationToken = default)
    {
        return _db.Achievements
            .AsNoTracking()
            .CountAsync(a => a.GameId == gameId, cancellationToken);
    }

    public async Task<IReadOnlyList<AchievementIdentityRow>> GetIdentityRowsAsync(
        IReadOnlyCollection<int> achievementIds,
        CancellationToken cancellationToken = default)
    {
        if (achievementIds.Count == 0)
        {
            return [];
        }

        return await _db.Achievements
            .AsNoTracking()
            .Where(a => achievementIds.Contains(a.Id))
            .Select(a => new AchievementIdentityRow
            {
                Id = a.Id,
                GameId = a.GameId
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AchievementCatalogRow>> GetCatalogAsync(
        int gameId,
        CancellationToken cancellationToken = default)
    {
        return await _db.Achievements
            .AsNoTracking()
            .Where(a => a.GameId == gameId)
            .Select(a => new AchievementCatalogRow
            {
                Id = a.Id,
                Name = a.Name,
                Description = a.Description,
                ImageUrlLock = a.ImageUrlLock,
                ImageUrlUnlock = a.ImageUrlUnlock,
                GlobalPercentage = a.GlobalPercentage
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResultDto<AchievementPageRow>> GetPagedForUserAsync(
        int userId,
        int gameId,
        AchievementPageQuery query,
        CancellationToken cancellationToken = default)
    {
        var baseQuery =
            from a in _db.Achievements.AsNoTracking()
            where a.GameId == gameId
            join ua in _db.UsersAchievements.AsNoTracking()
                    .Where(x => x.UserId == userId && x.GameId == gameId)
                on a.Id equals ua.AchievementId into unlocks
            from ua in unlocks.DefaultIfEmpty()
            select new AchievementPageRow
            {
                Id = a.Id,
                Name = a.Name,
                Description = a.Description,
                ImageUrlLock = a.ImageUrlLock,
                ImageUrlUnlock = a.ImageUrlUnlock,
                GlobalPercentage = a.GlobalPercentage,
                IsUnlocked = ua != null,
                UnlockUtc = ua != null ? ua.UnlockDate : null
            };

        var status = string.IsNullOrWhiteSpace(query.Status)
            ? "locked"
            : query.Status.Trim().ToLowerInvariant();
        if (status is not ("locked" or "unlocked" or "all" or ""))
        {
            status = "locked";
        }

        if (status is "all")
        {
            status = "";
        }

        if (status == "locked")
        {
            var anyLocked = await baseQuery.AnyAsync(r => !r.IsUnlocked, cancellationToken);
            if (!anyLocked)
            {
                status = "";
            }
        }

        if (!string.IsNullOrWhiteSpace(query.Name))
        {
            var pattern = $"%{query.Name.Trim()}%";
            baseQuery = baseQuery.Where(r =>
                EF.Functions.ILike(r.Name, pattern)
                || (r.Description != null && EF.Functions.ILike(r.Description, pattern)));
        }

        if (status == "unlocked")
        {
            baseQuery = baseQuery.Where(r => r.IsUnlocked);
        }
        else if (status == "locked")
        {
            baseQuery = baseQuery.Where(r => !r.IsUnlocked);
        }

        var global = string.IsNullOrWhiteSpace(query.Global)
            ? ""
            : query.Global.Trim().ToLowerInvariant();
        if (global is "all")
        {
            global = "";
        }

        baseQuery = global switch
        {
            "under1" => baseQuery.Where(r => r.GlobalPercentage != null && r.GlobalPercentage < 1m),
            "under5" => baseQuery.Where(r => r.GlobalPercentage != null && r.GlobalPercentage >= 1m && r.GlobalPercentage < 5m),
            "under10" => baseQuery.Where(r => r.GlobalPercentage != null && r.GlobalPercentage >= 5m && r.GlobalPercentage < 10m),
            "under30" => baseQuery.Where(r => r.GlobalPercentage != null && r.GlobalPercentage >= 10m && r.GlobalPercentage < 30m),
            "under50" => baseQuery.Where(r => r.GlobalPercentage != null && r.GlobalPercentage >= 30m && r.GlobalPercentage < 50m),
            "above50" => baseQuery.Where(r => r.GlobalPercentage != null && r.GlobalPercentage >= 50m),
            _ => baseQuery
        };

        var sort = query.Sort?.Trim().ToLowerInvariant() ?? "unlocktime";
        IOrderedQueryable<AchievementPageRow> ordered = sort switch
        {
            "name" => baseQuery.OrderBy(r => r.Name),
            "global" => baseQuery
                .OrderBy(r => r.GlobalPercentage == null ? 1 : 0)
                .ThenBy(r => r.GlobalPercentage)
                .ThenBy(r => r.Name),
            _ => baseQuery
                .OrderByDescending(r => r.IsUnlocked && r.UnlockUtc != null)
                .ThenByDescending(r => r.UnlockUtc)
                .ThenBy(r => r.Name)
        };

        var totalCount = await ordered.CountAsync(cancellationToken);
        var pagination = PaginationDto.Create(query.Page, query.PageSize, totalCount);
        var rows = await ordered
            .Skip(pagination.Skip)
            .Take(pagination.PerPage)
            .ToListAsync(cancellationToken);

        return PagedResultDto<AchievementPageRow>.From(rows, pagination);
    }

    public async Task<IReadOnlyList<Achievement>> GetTrackedByGameIdAsync(
        int gameId,
        CancellationToken cancellationToken = default)
    {
        return await _db.Achievements
            .Where(a => a.GameId == gameId)
            .ToListAsync(cancellationToken);
    }

    public void Add(Achievement achievement)
    {
        _db.Achievements.Add(achievement);
    }
}
