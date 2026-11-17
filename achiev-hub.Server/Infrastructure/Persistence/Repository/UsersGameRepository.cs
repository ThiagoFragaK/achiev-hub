using achiev_hub.Server.Application.Common;
using achiev_hub.Server.Application.Users.Interfaces;
using achiev_hub.Server.Domain.Entities;
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

    public Task<decimal?> GetAchievementPercentageAsync(
        int userId,
        int gameId,
        CancellationToken cancellationToken = default)
    {
        return _db.UsersGames
            .AsNoTracking()
            .Where(ug => ug.UserId == userId && ug.GameId == gameId)
            .Select(ug => (decimal?)ug.AchievementsPercentage)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<int, decimal>> GetAchievementPercentagesAsync(
        int userId,
        IReadOnlyCollection<int> gameIds,
        CancellationToken cancellationToken = default)
    {
        if (gameIds.Count == 0)
        {
            return new Dictionary<int, decimal>();
        }

        return await _db.UsersGames
            .AsNoTracking()
            .Where(ug => ug.UserId == userId && gameIds.Contains(ug.GameId))
            .ToDictionaryAsync(ug => ug.GameId, ug => ug.AchievementsPercentage, cancellationToken);
    }

    public Task<int> CountCommunityVisibleGamesAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        return _db.UsersGames
            .AsNoTracking()
            .CountAsync(
                ug => ug.UserId == userId && ug.Game.HasCommunityVisibleStats == true,
                cancellationToken);
    }

    public async Task<IReadOnlyList<decimal>> GetCommunityVisiblePercentagesAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        return await _db.UsersGames
            .AsNoTracking()
            .Where(ug => ug.UserId == userId && ug.Game.HasCommunityVisibleStats == true)
            .Select(ug => ug.AchievementsPercentage)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResultDto<LibraryGameRow>> GetLibraryPageAsync(
        int userId,
        LibraryGameQuery query,
        CancellationToken cancellationToken = default)
    {
        var dbQuery = _db.UsersGames
            .AsNoTracking()
            .Where(ug => ug.UserId == userId && ug.Game.ImageUrl != null && ug.Game.ImageUrl != "");

        if (!string.IsNullOrWhiteSpace(query.Name))
        {
            var name = query.Name.Trim().ToLowerInvariant();
            dbQuery = dbQuery.Where(ug => ug.Game.Name.ToLower().Contains(name));
        }

        if (query.MinHours is double minHours)
        {
            var minMinutes = (int)Math.Ceiling(minHours * 60);
            dbQuery = dbQuery.Where(ug => ug.PlaytimeMinutes >= minMinutes);
        }

        if (query.HasAchievements is bool hasAchievements)
        {
            dbQuery = dbQuery.Where(ug => ug.Game.HasCommunityVisibleStats == hasAchievements);
        }

        var sort = query.Sort?.Trim().ToLowerInvariant() ?? "playtime";
        dbQuery = sort switch
        {
            "name" => dbQuery.OrderBy(ug => ug.Game.Name),
            "achievements" => dbQuery
                .OrderByDescending(ug => ug.Game.HasCommunityVisibleStats == true)
                .ThenByDescending(ug => ug.AchievementsPercentage)
                .ThenBy(ug => ug.Game.Name),
            _ => dbQuery
                .OrderByDescending(ug => ug.PlaytimeMinutes)
                .ThenBy(ug => ug.Game.Name)
        };

        var totalCount = await dbQuery.CountAsync(cancellationToken);
        var pagination = PaginationDto.Create(query.Page, query.PageSize, totalCount);

        var items = await dbQuery
            .Skip(pagination.Skip)
            .Take(pagination.PerPage)
            .Select(ug => new LibraryGameRow
            {
                GameSteamId = ug.Game.GameSteamId,
                Name = ug.Game.Name,
                ImageUrl = ug.Game.ImageUrl,
                PlaytimeMinutes = ug.PlaytimeMinutes,
                LastPlayedUnix = ug.LastPlayedUnix,
                HasAchievements = ug.Game.HasCommunityVisibleStats == true,
                AchievementsPercentage = ug.AchievementsPercentage
            })
            .ToListAsync(cancellationToken);

        return PagedResultDto<LibraryGameRow>.From(items, pagination);
    }

    public Task<UsersGame?> GetTrackedByUserAndGameAsync(
        int userId,
        int gameId,
        CancellationToken cancellationToken = default)
    {
        return _db.UsersGames
            .FirstOrDefaultAsync(ug => ug.UserId == userId && ug.GameId == gameId, cancellationToken);
    }

    public async Task<IReadOnlyList<UsersGame>> GetTrackedByUserAndGameIdsAsync(
        int userId,
        IReadOnlyCollection<int> gameIds,
        CancellationToken cancellationToken = default)
    {
        if (gameIds.Count == 0)
        {
            return [];
        }

        return await _db.UsersGames
            .Where(ug => ug.UserId == userId && gameIds.Contains(ug.GameId))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetOwnedWithStatsSteamIdsAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        return await _db.UsersGames
            .AsNoTracking()
            .Where(ug => ug.UserId == userId && ug.Game.HasCommunityVisibleStats == true)
            .Select(ug => ug.Game.GameSteamId)
            .ToListAsync(cancellationToken);
    }

    public void Add(UsersGame usersGame)
    {
        _db.UsersGames.Add(usersGame);
    }
}
