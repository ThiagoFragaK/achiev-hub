using achiev_hub.Server.Data;
using achiev_hub.Server.DTOs;
using achiev_hub.Server.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace achiev_hub.Server.Services;

public class UserStatsService : IUserStatsService
{
    private const int DaysWindow = 14;

    private static readonly TimeZoneInfo DateTimeZone = TimeZoneInfo.Local;

    private readonly ApplicationDbContext _db;

    public UserStatsService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<UserStatsDto> GetStatsAsync(int userId, CancellationToken cancellationToken = default)
    {
        // A single pass over the user's unlock timestamps feeds both charts.
        var unlockDates = await _db.UsersAchievements
            .AsNoTracking()
            .Where(ua => ua.UserId == userId && ua.UnlockDate != null)
            .Select(ua => ua.UnlockDate!.Value)
            .ToListAsync(cancellationToken);

        var localDays = unlockDates.Select(ToLocalDate).ToList();

        var userStats = await _db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.AvgPercentage, u.AchievementSyncCoverage })
            .FirstOrDefaultAsync(cancellationToken);

        var ownedWithStats = await _db.UsersGames.AsNoTracking()
            .CountAsync(ug => ug.UserId == userId && ug.Game.HasCommunityVisibleStats == true, cancellationToken);
        var syncedWithStats = await _db.UsersGames.AsNoTracking()
            .CountAsync(
                ug => ug.UserId == userId
                    && ug.Game.HasCommunityVisibleStats == true
                    && ug.AchievementsSyncedAt != null,
                cancellationToken);

        return new UserStatsDto
        {
            AchievementsLast14Days = BuildLastDays(localDays),
            AchievementsPerYear = BuildPerYear(localDays),
            AveragePercentage = userStats?.AvgPercentage ?? 0,
            AchievementSyncCoverage = userStats?.AchievementSyncCoverage ?? 0,
            OwnedWithStats = ownedWithStats,
            SyncedWithStats = syncedWithStats
        };
    }

    public UserStatsDto GetEmptyStats()
    {
        return new UserStatsDto
        {
            AchievementsLast14Days = BuildLastDays([]),
            AchievementsPerYear = [],
            AveragePercentage = 0,
            AchievementSyncCoverage = 0,
            OwnedWithStats = 0,
            SyncedWithStats = 0
        };
    }

    public async Task<GameProgressDto> GetGameProgressAsync(int userId, int appId, CancellationToken cancellationToken = default)
    {
        var steamAppId = appId.ToString();
        var gameId = await _db.Games
            .AsNoTracking()
            .Where(g => g.GameSteamId == steamAppId)
            .Select(g => (int?)g.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (gameId is not int id)
        {
            return GetEmptyGameProgress();
        }

        var totalCount = await _db.Achievements
            .AsNoTracking()
            .CountAsync(a => a.GameId == id, cancellationToken);

        var unlockDates = await _db.UsersAchievements
            .AsNoTracking()
            .Where(ua => ua.UserId == userId && ua.GameId == id)
            .Select(ua => ua.UnlockDate)
            .ToListAsync(cancellationToken);

        var unlockedCount = unlockDates.Count;
        var isCompleted = totalCount > 0 && unlockedCount >= totalCount;

        // Steam occasionally reports an unlock without a timestamp; those cannot be placed on the timeline.
        var unlockDays = unlockDates
            .Where(date => date.HasValue)
            .Select(date => ToLocalDate(date!.Value))
            .OrderBy(day => day)
            .ToList();

        if (unlockDays.Count == 0)
        {
            return new GameProgressDto
            {
                Points = [],
                Granularity = GameProgressGranularity.Day,
                UnlockedCount = unlockedCount,
                TotalCount = totalCount,
                IsCompleted = isCompleted
            };
        }

        var start = unlockDays[0];

        // While anything is still locked the run is ongoing, so the timeline reaches today.
        // Once everything is unlocked it ends on the day the last achievement was unlocked.
        var end = isCompleted ? unlockDays[^1] : ToLocalDate(DateTime.UtcNow);
        if (end < start)
        {
            end = start;
        }

        var granularity = ChooseGranularity(start, end);

        return new GameProgressDto
        {
            Points = BuildProgressPoints(unlockDays, start, end, granularity, totalCount),
            Granularity = granularity,
            UnlockedCount = unlockedCount,
            TotalCount = totalCount,
            IsCompleted = isCompleted
        };
    }

    public GameProgressDto GetEmptyGameProgress()
    {
        return new GameProgressDto
        {
            Points = [],
            Granularity = GameProgressGranularity.Day
        };
    }

    private static IReadOnlyList<GameProgressPointDto> BuildProgressPoints(
        IReadOnlyList<DateTime> sortedUnlockDays,
        DateTime start,
        DateTime end,
        string granularity,
        int totalCount)
    {
        var points = new List<GameProgressPointDto>();
        var unlockIndex = 0;
        var cumulative = 0;

        foreach (var day in BuildTimeline(start, end, granularity))
        {
            while (unlockIndex < sortedUnlockDays.Count && sortedUnlockDays[unlockIndex] <= day)
            {
                cumulative++;
                unlockIndex++;
            }

            points.Add(new GameProgressPointDto
            {
                Date = day.ToString("yyyy-MM-dd"),
                Count = cumulative,
                Percentage = totalCount == 0
                    ? 0
                    : Math.Round(cumulative / (decimal)totalCount * 100, 2)
            });
        }

        return points;
    }

    private static IEnumerable<DateTime> BuildTimeline(DateTime start, DateTime end, string granularity)
    {
        var day = start;
        while (day < end)
        {
            yield return day;
            day = granularity switch
            {
                GameProgressGranularity.Day => day.AddDays(1),
                GameProgressGranularity.Week => day.AddDays(7),
                GameProgressGranularity.Month => day.AddMonths(1),
                _ => day.AddYears(1)
            };
        }

        // The end date always closes the timeline, even when the last step overshoots it.
        yield return end;
    }

    private static string ChooseGranularity(DateTime start, DateTime end)
    {
        // Steps are picked so a run keeps a readable number of points whatever its length.
        var days = (end - start).TotalDays;
        return days switch
        {
            <= 60 => GameProgressGranularity.Day,
            <= 730 => GameProgressGranularity.Week,
            <= 3650 => GameProgressGranularity.Month,
            _ => GameProgressGranularity.Year
        };
    }

    private static IReadOnlyList<DailyAchievementCountDto> BuildLastDays(IReadOnlyCollection<DateTime> localDays)
    {
        var today = ToLocalDate(DateTime.UtcNow);
        var first = today.AddDays(-(DaysWindow - 1));

        var countsByDay = localDays
            .Where(day => day >= first && day <= today)
            .GroupBy(day => day)
            .ToDictionary(group => group.Key, group => group.Count());

        return Enumerable.Range(0, DaysWindow)
            .Select(offset =>
            {
                var day = first.AddDays(offset);
                return new DailyAchievementCountDto
                {
                    Date = day.ToString("yyyy-MM-dd"),
                    Count = countsByDay.TryGetValue(day, out var count) ? count : 0
                };
            })
            .ToList();
    }

    private static IReadOnlyList<YearlyAchievementCountDto> BuildPerYear(IReadOnlyCollection<DateTime> localDays)
    {
        if (localDays.Count == 0)
        {
            return [];
        }

        var countsByYear = localDays
            .GroupBy(day => day.Year)
            .ToDictionary(group => group.Key, group => group.Count());

        var firstYear = countsByYear.Keys.Min();
        var lastYear = countsByYear.Keys.Max();

        // Years without unlocks are kept as zeros so the area chart stays continuous.
        return Enumerable.Range(firstYear, lastYear - firstYear + 1)
            .Select(year => new YearlyAchievementCountDto
            {
                Year = year,
                Count = countsByYear.TryGetValue(year, out var count) ? count : 0
            })
            .ToList();
    }

    private static DateTime ToLocalDate(DateTime storedUtc)
    {
        var utc = storedUtc.Kind == DateTimeKind.Utc
            ? storedUtc
            : DateTime.SpecifyKind(storedUtc, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeFromUtc(utc, DateTimeZone).Date;
    }
}
