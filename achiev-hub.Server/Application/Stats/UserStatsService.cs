using achiev_hub.Server.Application.Achievements.Interfaces;
using achiev_hub.Server.Application.Games;
using achiev_hub.Server.Application.Games.Interfaces;
using achiev_hub.Server.Application.Stats.Interfaces;
using achiev_hub.Server.Application.Users.Interfaces;

namespace achiev_hub.Server.Application.Stats;

public class UserStatsService : IUserStatsService
{
    private const int DaysWindow = 14;

    private static readonly TimeZoneInfo DateTimeZone = TimeZoneInfo.Local;

    private readonly IUsersGameRepository _usersGames;
    private readonly IUsersAchievementRepository _usersAchievements;
    private readonly IGameRepository _games;
    private readonly IAchievementRepository _achievements;
    private readonly IUserRepository _users;

    public UserStatsService(
        IUsersGameRepository usersGames,
        IUsersAchievementRepository usersAchievements,
        IGameRepository games,
        IAchievementRepository achievements,
        IUserRepository users)
    {
        _usersGames = usersGames;
        _usersAchievements = usersAchievements;
        _games = games;
        _achievements = achievements;
        _users = users;
    }

    public async Task<UserStatsDto> GetStatsAsync(int userId, CancellationToken cancellationToken = default)
    {
        var sinceUtc = DateTime.UtcNow.Date.AddDays(-(DaysWindow - 1));

        var recentDayCounts = await _usersAchievements.GetDailyUnlockCountsAsync(
            userId,
            sinceUtc,
            cancellationToken);

        var yearCounts = await _usersAchievements.GetYearlyUnlockCountsAsync(userId, cancellationToken);

        var averagePercentage = await _users.GetAveragePercentageAsync(userId, cancellationToken);
        var totalAchievements = await _usersAchievements.CountForUserAsync(userId, cancellationToken);
        var gamesWithAchievements = await _usersGames.CountCommunityVisibleGamesAsync(
            userId,
            cancellationToken);

        return new UserStatsDto
        {
            AchievementsLast14Days = BuildLastDaysFromCounts(
                recentDayCounts.ToDictionary(r => ToLocalDate(r.Day), r => r.Count)),
            AchievementsPerYear = BuildPerYearFromCounts(
                yearCounts.ToDictionary(r => r.Year, r => r.Count)),
            AveragePercentage = averagePercentage ?? 0,
            TotalAchievements = totalAchievements,
            GamesWithAchievements = gamesWithAchievements
        };
    }

    public UserStatsDto GetEmptyStats()
    {
        return new UserStatsDto
        {
            AchievementsLast14Days = BuildLastDays([]),
            AchievementsPerYear = [],
            AveragePercentage = 0,
            TotalAchievements = 0,
            GamesWithAchievements = 0
        };
    }

    public async Task<GameProgressDto> GetGameProgressAsync(int userId, int appId, CancellationToken cancellationToken = default)
    {
        var gameId = await _games.GetIdBySteamIdAsync(appId.ToString(), cancellationToken);

        if (gameId is not int id)
        {
            return GetEmptyGameProgress();
        }

        var totalCount = await _achievements.CountForGameAsync(id, cancellationToken);
        var unlockDates = await _usersAchievements.GetUnlockDatesListAsync(userId, id, cancellationToken);

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

    public async Task<AchievementsByMonthDto> GetAchievementsByMonthAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var monthCounts = await _usersAchievements.GetMonthlyUnlockCountsAsync(userId, cancellationToken);

        return BuildAchievementsByMonthFromCounts(
            monthCounts.ToDictionary(r => (r.Year, r.Month), r => r.Count));
    }

    public AchievementsByMonthDto GetEmptyAchievementsByMonth()
    {
        return new AchievementsByMonthDto
        {
            AvailableYears = [],
            Series = []
        };
    }

    public async Task<GamesPercentageBandsDto> GetGamesPercentageBandsAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var bands = await _usersGames.GetPercentageBandCountsAsync(userId, cancellationToken);
        return new GamesPercentageBandsDto
        {
            Bands = bands
                .Select(b => new PercentageBandCountDto
                {
                    Key = b.Key,
                    Label = b.Label,
                    Count = b.Count
                })
                .ToList()
        };
    }

    public GamesPercentageBandsDto GetEmptyGamesPercentageBands()
    {
        return new GamesPercentageBandsDto
        {
            Bands = PercentageBandDefs
                .Select(def => new PercentageBandCountDto
                {
                    Key = def.Key,
                    Label = def.Label,
                    Count = 0
                })
                .ToList()
        };
    }

    public async Task<GamesForAverageDto> GetGamesForAverageAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var (currentAverage, gamesInAverage) = await _usersGames.GetAveragePercentageStatsAsync(
            userId,
            cancellationToken);

        var rows = await _usersGames.GetGamesWithAchievementStatsAsync(
            userId,
            excludeCompleted: true,
            cancellationToken: cancellationToken);

        var games = new List<GameForAverageDto>();
        foreach (var row in rows)
        {
            if (!int.TryParse(row.GameSteamId, out var appId))
            {
                continue;
            }

            games.Add(new GameForAverageDto
            {
                AppId = appId,
                Name = row.Name,
                AchievementsPercentage = row.AchievementsPercentage
            });
        }

        return new GamesForAverageDto
        {
            CurrentAverage = currentAverage,
            GamesInAverage = gamesInAverage,
            Games = games
        };
    }

    public GamesForAverageDto GetEmptyGamesForAverage()
    {
        return new GamesForAverageDto
        {
            CurrentAverage = 0,
            GamesInAverage = 0,
            Games = []
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
        GameProgressPointDto? lastPoint = null;

        foreach (var day in BuildTimeline(start, end, granularity))
        {
            while (unlockIndex < sortedUnlockDays.Count && sortedUnlockDays[unlockIndex] <= day)
            {
                cumulative++;
                unlockIndex++;
            }

            var percentage = totalCount == 0
                ? 0
                : Math.Round(cumulative / (decimal)totalCount * 100, 2);

            var point = new GameProgressPointDto
            {
                Date = day.ToString("yyyy-MM-dd"),
                Count = cumulative,
                Percentage = percentage
            };

            var isEnd = day == end;
            var percentageChanged = lastPoint is null || lastPoint.Percentage != percentage;

            // Keep first point, every % change, and always close the series on end/today.
            if (percentageChanged || isEnd)
            {
                // Avoid duplicating the end date when it already matched the last change.
                if (isEnd && lastPoint is not null && lastPoint.Date == point.Date)
                {
                    continue;
                }

                points.Add(point);
                lastPoint = point;
            }
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
        var countsByDay = localDays
            .GroupBy(day => day)
            .ToDictionary(group => group.Key, group => group.Count());
        return BuildLastDaysFromCounts(countsByDay);
    }

    private static IReadOnlyList<DailyAchievementCountDto> BuildLastDaysFromCounts(
        IReadOnlyDictionary<DateTime, int> countsByDay)
    {
        var today = ToLocalDate(DateTime.UtcNow);
        var first = today.AddDays(-(DaysWindow - 1));

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

    private static IReadOnlyList<YearlyAchievementCountDto> BuildPerYearFromCounts(
        IReadOnlyDictionary<int, int> countsByYear)
    {
        if (countsByYear.Count == 0)
        {
            return [];
        }

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

    private static AchievementsByMonthDto BuildAchievementsByMonthFromCounts(
        IReadOnlyDictionary<(int Year, int Month), int> countsByYearMonth)
    {
        if (countsByYearMonth.Count == 0)
        {
            return new AchievementsByMonthDto
            {
                AvailableYears = [],
                Series = []
            };
        }

        var years = countsByYearMonth.Keys
            .Select(key => key.Year)
            .Distinct()
            .OrderByDescending(year => year)
            .ToList();

        var series = years
            .Select(year =>
            {
                var counts = new int[12];
                for (var month = 1; month <= 12; month++)
                {
                    counts[month - 1] = countsByYearMonth.TryGetValue((year, month), out var count)
                        ? count
                        : 0;
                }

                return new YearMonthSeriesDto
                {
                    Year = year,
                    Counts = counts
                };
            })
            .ToList();

        return new AchievementsByMonthDto
        {
            AvailableYears = years,
            Series = series
        };
    }

    private static readonly (string Key, string Label, Func<decimal, bool> Match)[] PercentageBandDefs =
    [
        ("100", "100%", p => p == 100m),
        ("gt80", "> 80%", p => p > 80m && p < 100m),
        ("gt60", "> 60%", p => p > 60m && p <= 80m),
        ("gt50", "> 50%", p => p > 50m && p <= 60m),
        ("gt30", "> 30%", p => p > 30m && p <= 50m),
        ("gt10", "> 10%", p => p > 10m && p <= 30m),
        ("gt0", "> 0%", p => p > 0m && p <= 10m),
        ("0", "0%", p => p == 0m)
    ];

    private static GamesPercentageBandsDto BuildPercentageBands(IReadOnlyCollection<decimal> percentages)
    {
        return new GamesPercentageBandsDto
        {
            Bands = PercentageBandDefs
                .Select(def => new PercentageBandCountDto
                {
                    Key = def.Key,
                    Label = def.Label,
                    Count = percentages.Count(def.Match)
                })
                .ToList()
        };
    }

    private static DateTime ToLocalDate(DateTime storedUtc)
    {
        var utc = storedUtc.Kind == DateTimeKind.Utc
            ? storedUtc
            : DateTime.SpecifyKind(storedUtc, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeFromUtc(utc, DateTimeZone).Date;
    }
}
