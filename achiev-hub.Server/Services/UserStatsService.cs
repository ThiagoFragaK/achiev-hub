using achiev_hub.Server.Data;
using achiev_hub.Server.DTOs;
using achiev_hub.Server.Repositories.Interfaces;
using achiev_hub.Server.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SteamSync.Shared;

namespace achiev_hub.Server.Services;

public class UserStatsService : IUserStatsService
{
    private const int DaysWindow = 14;
    private static readonly TimeSpan LiveFallbackWindow = TimeSpan.FromHours(24);
    private static readonly TimeSpan SteamLiveCacheTtl = TimeSpan.FromSeconds(60);

    private static readonly TimeZoneInfo DateTimeZone = TimeZoneInfo.Local;

    private readonly ApplicationDbContext _db;
    private readonly ISteamRepository _steamRepository;
    private readonly IMemoryCache _cache;

    public UserStatsService(
        ApplicationDbContext db,
        ISteamRepository steamRepository,
        IMemoryCache cache)
    {
        _db = db;
        _steamRepository = steamRepository;
        _cache = cache;
    }

    public async Task<UserStatsDto> GetStatsAsync(int userId, CancellationToken cancellationToken = default)
    {
        var syncRow = await _db.UserSyncStatuses.AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);

        var unlockDates = await _db.UsersAchievements
            .AsNoTracking()
            .Where(ua => ua.UserId == userId && ua.UnlockDate != null)
            .Select(ua => ua.UnlockDate!.Value)
            .ToListAsync(cancellationToken);

        var localDays = unlockDates.Select(ToLocalDate).ToList();

        var userStats = await _db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.AvgPercentage, u.AchievementSyncCoverage, u.SteamId })
            .FirstOrDefaultAsync(cancellationToken);

        var ownedWithStats = await _db.UsersGames.AsNoTracking()
            .CountAsync(ug => ug.UserId == userId && ug.Game.HasCommunityVisibleStats == true, cancellationToken);
        var syncedWithStats = await _db.UsersGames.AsNoTracking()
            .CountAsync(
                ug => ug.UserId == userId
                    && ug.Game.HasCommunityVisibleStats == true
                    && ug.AchievementsSyncedAt != null
                    && !ug.AchievementSyncUnavailable,
                cancellationToken);

        var sync = syncRow is null
            ? SyncSummaryDto.Empty
            : SyncSummaryDto.From(
                syncRow.Status,
                syncRow.LastFullSync,
                syncRow.LastPartialSync,
                syncRow.GamesSyncedCount > 0 ? syncRow.GamesSyncedCount : syncedWithStats,
                syncRow.TotalGamesCount > 0 ? syncRow.TotalGamesCount : ownedWithStats,
                syncRow.SyncProgressPercent);

        // Live fallback only while pending/syncing within 24h and no synced achievements yet.
        var useLive = syncedWithStats == 0
            && !string.IsNullOrWhiteSpace(userStats?.SteamId)
            && ShouldUseLiveFallback(syncRow);

        if (useLive)
        {
            var liveOwned = await GetCachedOwnedCountAsync(userStats!.SteamId!, cancellationToken);
            return new UserStatsDto
            {
                AchievementsLast14Days = BuildLastDays([]),
                AchievementsPerYear = [],
                AveragePercentage = 0,
                AchievementSyncCoverage = 0,
                OwnedWithStats = liveOwned,
                SyncedWithStats = 0,
                Sync = sync,
                Source = "steam_live",
                Fallback = true
            };
        }

        return new UserStatsDto
        {
            AchievementsLast14Days = BuildLastDays(localDays),
            AchievementsPerYear = BuildPerYear(localDays),
            AveragePercentage = userStats?.AvgPercentage ?? 0,
            AchievementSyncCoverage = userStats?.AchievementSyncCoverage ?? 0,
            OwnedWithStats = ownedWithStats,
            SyncedWithStats = syncedWithStats,
            Sync = sync,
            Source = "db",
            Fallback = false
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
            SyncedWithStats = 0,
            Sync = SyncSummaryDto.Empty,
            Source = "db"
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

    private async Task<int> GetCachedOwnedCountAsync(string steamId, CancellationToken cancellationToken)
    {
        var cacheKey = $"stats-live-owned:{steamId}";
        if (_cache.TryGetValue(cacheKey, out int cached))
        {
            return cached;
        }

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(5));
            var owned = await _steamRepository.GetOwnedGamesAsync(steamId, cts.Token);
            var count = owned.Count(g => g.HasCommunityVisibleStats);
            _cache.Set(cacheKey, count, SteamLiveCacheTtl);
            return count;
        }
        catch
        {
            return 0;
        }
    }

    private static bool ShouldUseLiveFallback(Entities.UserSyncStatus? syncRow)
    {
        if (syncRow is null)
        {
            return true;
        }

        if (syncRow.Status is not (SyncStatus.Pending or SyncStatus.Syncing))
        {
            return false;
        }

        return DateTimeOffset.UtcNow - syncRow.UpdatedAt <= LiveFallbackWindow;
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

        yield return end;
    }

    private static string ChooseGranularity(DateTime start, DateTime end)
    {
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
