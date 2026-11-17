using achiev_hub.Server.Application.Achievements.Interfaces;
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
