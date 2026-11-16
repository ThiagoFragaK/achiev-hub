using achiev_hub.Server.Application.Users.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace achiev_hub.Server.Infrastructure.Persistence.Repository;

public class UsersGameRepository : IUsersGameRepository
{
    private readonly ApplicationDbContext _db;

    public UsersGameRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<UsersGameAchievementStatsRow>> GetGamesWithAchievementStatsAsync(
        int userId,
        bool excludeCompleted = false,
        string? nameQuery = null,
        IReadOnlyCollection<int>? excludeGameIds = null,
        int? take = null,
        CancellationToken cancellationToken = default)
    {
        var query = _db.UsersGames
            .AsNoTracking()
            .Where(ug =>
                ug.UserId == userId
                && ug.Game.HasCommunityVisibleStats == true
                && ug.Game.Achievements.Any());

        if (excludeCompleted)
        {
            query = query.Where(ug => ug.AchievementsPercentage < 100m);
        }

        if (excludeGameIds is { Count: > 0 })
        {
            var excluded = excludeGameIds is HashSet<int> set
                ? set
                : excludeGameIds.ToHashSet();
            query = query.Where(ug => !excluded.Contains(ug.GameId));
        }

        if (!string.IsNullOrWhiteSpace(nameQuery))
        {
            var q = nameQuery.Trim().ToLowerInvariant();
            query = query.Where(ug => ug.Game.Name.ToLower().Contains(q));
        }

        query = query.OrderBy(ug => ug.Game.Name);

        var projected = query.Select(ug => new UsersGameAchievementStatsRow
        {
            GameId = ug.GameId,
            GameSteamId = ug.Game.GameSteamId,
            Name = ug.Game.Name,
            ImageUrl = ug.Game.ImageUrl,
            AchievementsPercentage = ug.AchievementsPercentage
        });

        if (take is > 0)
        {
            projected = projected.Take(take.Value);
        }

        return await projected.ToListAsync(cancellationToken);
    }

    public async Task<(decimal Average, int GamesInAverage)> GetAveragePercentageStatsAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var percentages = await _db.UsersGames
            .AsNoTracking()
            .Where(ug =>
                ug.UserId == userId
                && ug.Game.HasCommunityVisibleStats == true
                && ug.AchievementsPercentage > 0)
            .Select(ug => ug.AchievementsPercentage)
            .ToListAsync(cancellationToken);

        if (percentages.Count == 0)
        {
            return (0m, 0);
        }

        var average = Math.Round(percentages.Average(), 2);
        return (average, percentages.Count);
    }
}
