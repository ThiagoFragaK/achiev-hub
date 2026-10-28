using achiev_hub.Server.Infrastructure.Persistence;
using achiev_hub.Server.Application.Games;
using achiev_hub.Server.Application.Games.Interfaces;
using achiev_hub.Server.Application.Achievements;
using achiev_hub.Server.Application.Achievements.Interfaces;
using achiev_hub.Server.Application.Steam;
using achiev_hub.Server.Application.Steam.Interfaces;
using achiev_hub.Server.Application.Stats;
using achiev_hub.Server.Application.Stats.Interfaces;
using achiev_hub.Server.Application.Users;
using achiev_hub.Server.Application.Users.Interfaces;
using achiev_hub.Server.Application.Goals;
using achiev_hub.Server.Application.Goals.Interfaces;
using achiev_hub.Server.Application.Auth;
using achiev_hub.Server.Application.Auth.Interfaces;
using achiev_hub.Server.Application.Common;
using achiev_hub.Server.Application.Common.Interfaces;
using achiev_hub.Server.Infrastructure.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace achiev_hub.Server.Application.Games;

public class GamesService : IGamesService
{
    private static readonly TimeZoneInfo DateTimeZone = TimeZoneInfo.Local;

    private readonly ISteamApiClient _steamApiClient;
    private readonly ApplicationDbContext _db;
    private readonly IMemoryCache _cache;

    public GamesService(ISteamApiClient steamApiClient, ApplicationDbContext db, IMemoryCache cache)
    {
        _steamApiClient = steamApiClient;
        _db = db;
        _cache = cache;
    }

    public async Task<PagedResultDto<RecentGameDto>> GetRecentGamesAsync(
        string steamId,
        int page,
        int pageSize,
        int? userId = null,
        CancellationToken cancellationToken = default)
    {
        // Recent games always come from Steam (auth + guest), with a short memory cache.
        _ = userId;
        var mapped = await GetOrFetchRecentGamesAsync(steamId, cancellationToken);
        return Paginate(mapped, page, pageSize);
    }

    public async Task<PagedResultDto<LibraryGameDto>> GetLibraryAsync(
        string steamId,
        int page,
        int pageSize,
        int? userId = null,
        LibraryGameFilterDto? filters = null,
        CancellationToken cancellationToken = default)
    {
        if (userId is int authUserId)
        {
            return await GetLibraryFromDbAsync(authUserId, page, pageSize, filters, cancellationToken);
        }

        return await GetLibraryFromSteamAsync(steamId, page, pageSize, filters, cancellationToken);
    }

    public async Task<GameDetailsDto?> GetGameDetailsAsync(int appId, CancellationToken cancellationToken = default)
    {
        var storeGame = await _steamApiClient.GetStoreGameDetailsAsync(appId, cancellationToken);
        if (storeGame is null)
        {
            return null;
        }

        return new GameDetailsDto
        {
            GameName = storeGame.Name,
            GameImage = SteamMediaUrls.HeaderImageUrl(appId.ToString()) ?? storeGame.HeaderImage,
            Developers = string.Join(",", storeGame.Developers),
            Publishers = string.Join(",", storeGame.Publishers)
        };
    }

    public async Task<PagedResultDto<AchievementDto>> GetAchievementsAsync(
        string steamId,
        int appId,
        int page,
        int pageSize,
        string? name = null,
        string? status = null,
        string? sort = null,
        string? global = null,
        int? userId = null,
        CancellationToken cancellationToken = default)
    {
        if (userId is int authUserId)
        {
            return await GetAchievementsFromDbAsync(authUserId, appId, page, pageSize, name, status, sort, global, cancellationToken);
        }

        var playerResult = await _steamApiClient.GetPlayerAchievementsAsync(steamId, appId, cancellationToken);
        var schema = await _steamApiClient.GetGameSchemaAsync(appId, cancellationToken);

        if (playerResult is null || !playerResult.Success)
        {
            throw new SteamApiException();
        }

        var schemaByName = (schema?.Achievements ?? [])
            .Where(achievement => !string.IsNullOrWhiteSpace(achievement.Name))
            .GroupBy(achievement => achievement.Name!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        var rows = new List<AchievementSortRow>();
        foreach (var playerAchievement in playerResult.Achievements)
        {
            if (string.IsNullOrWhiteSpace(playerAchievement.ApiName))
            {
                continue;
            }

            var unlocked = playerAchievement.Achieved == 1;
            schemaByName.TryGetValue(playerAchievement.ApiName, out var schemaAchievement);
            var displayName = string.IsNullOrWhiteSpace(schemaAchievement?.DisplayName)
                ? playerAchievement.ApiName
                : schemaAchievement.DisplayName;

            rows.Add(new AchievementSortRow
            {
                IsUnlocked = unlocked,
                UnlockUtc = unlocked && playerAchievement.UnlockTimeUnix > 0
                    ? DateTimeOffset.FromUnixTimeSeconds(playerAchievement.UnlockTimeUnix).UtcDateTime
                    : null,
                Dto = new AchievementDto
                {
                    Name = displayName,
                    Description = schemaAchievement is null
                        ? null
                        : schemaAchievement.Hidden == 1
                            ? "Secret achievement: without description"
                            : schemaAchievement.Description,
                    Icon = unlocked
                        ? SteamMediaUrls.AppIconUrl(appId.ToString(), schemaAchievement?.Icon)
                        : SteamMediaUrls.AppIconUrl(appId.ToString(), schemaAchievement?.IconGray),
                    Unlocked = unlocked ? FormatDateTime(playerAchievement.UnlockTimeUnix) : "-"
                }
            });
        }

        return Paginate(FilterAndSortAchievements(rows, name, status, sort, global), page, pageSize);
    }

    public async Task<PagedResultDto<RecentAchievementDto>> GetRecentAchievementsAsync(
        int userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        const int maxRecent = 100;
        if (pageSize <= 0)
        {
            pageSize = 10;
        }

        pageSize = Math.Min(pageSize, 10);

        var unlockQuery = _db.UsersAchievements
            .AsNoTracking()
            .Where(ua => ua.UserId == userId && ua.UnlockDate != null);

        var unlockedCount = await unlockQuery.CountAsync(cancellationToken);
        var totalCount = Math.Min(unlockedCount, maxRecent);
        if (totalCount == 0)
        {
            return new PagedResultDto<RecentAchievementDto>
            {
                Data = [],
                CurrentPage = 1,
                LastPage = 1,
                PerPage = pageSize,
                TotalCount = 0
            };
        }

        var lastPage = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
        var currentPage = page <= 0 ? 1 : Math.Min(page, lastPage);
        var skip = (currentPage - 1) * pageSize;
        var take = Math.Min(pageSize, totalCount - skip);

        var rows = await unlockQuery
            .OrderByDescending(ua => ua.UnlockDate)
            .Skip(skip)
            .Take(take)
            .Select(ua => new
            {
                AchievementName = ua.Achievement.Name,
                ua.Achievement.Description,
                ua.Achievement.ImageUrlUnlock,
                ua.Achievement.GlobalPercentage,
                ua.UnlockDate,
                GameName = ua.Game.Name,
                ua.Game.GameSteamId
            })
            .ToListAsync(cancellationToken);

        var data = rows.Select(row =>
        {
            var appId = int.TryParse(row.GameSteamId, out var parsed) ? parsed : 0;
            return new RecentAchievementDto
            {
                Name = row.AchievementName,
                Description = row.Description,
                Icon = SteamMediaUrls.AppIconUrl(row.GameSteamId, row.ImageUrlUnlock),
                AppId = appId,
                GameName = row.GameName,
                Unlocked = row.UnlockDate.HasValue
                    ? FormatDateTimeFromUtc(row.UnlockDate.Value)
                    : "-",
                GlobalPercentage = row.GlobalPercentage
            };
        }).ToList();

        return new PagedResultDto<RecentAchievementDto>
        {
            Data = data,
            CurrentPage = currentPage,
            LastPage = lastPage,
            PerPage = pageSize,
            TotalCount = totalCount
        };
    }

    private async Task<PagedResultDto<AchievementDto>> GetAchievementsFromDbAsync(
        int userId,
        int appId,
        int page,
        int pageSize,
        string? name,
        string? status,
        string? sort,
        string? global,
        CancellationToken cancellationToken)
    {
        var steamAppId = appId.ToString();
        var gameId = await _db.Games
            .AsNoTracking()
            .Where(g => g.GameSteamId == steamAppId)
            .Select(g => (int?)g.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (gameId is not int id)
        {
            return Paginate(Array.Empty<AchievementDto>(), page, pageSize);
        }

        var unlocks = await _db.UsersAchievements
            .AsNoTracking()
            .Where(ua => ua.UserId == userId && ua.GameId == id)
            .Select(ua => new { ua.AchievementId, ua.UnlockDate })
            .ToDictionaryAsync(ua => ua.AchievementId, cancellationToken);

        var catalog = await _db.Achievements
            .AsNoTracking()
            .Where(a => a.GameId == id)
            .Select(a => new
            {
                a.Id,
                a.Name,
                a.Description,
                a.ImageUrlLock,
                a.ImageUrlUnlock,
                a.GlobalPercentage
            })
            .ToListAsync(cancellationToken);

        var rows = new List<AchievementSortRow>(catalog.Count);
        foreach (var achievement in catalog)
        {
            var unlocked = unlocks.TryGetValue(achievement.Id, out var usersUnlock);
            DateTime? unlockUtc = null;
            var unlockDate = "-";
            if (unlocked && usersUnlock is not null && usersUnlock.UnlockDate.HasValue)
            {
                unlockUtc = DateTime.SpecifyKind(usersUnlock.UnlockDate.Value, DateTimeKind.Utc);
                unlockDate = FormatDateTimeFromUtc(usersUnlock.UnlockDate.Value);
            }

            var iconHash = unlocked ? achievement.ImageUrlUnlock : achievement.ImageUrlLock;
            rows.Add(new AchievementSortRow
            {
                IsUnlocked = unlocked,
                UnlockUtc = unlockUtc,
                Dto = new AchievementDto
                {
                    Name = achievement.Name,
                    Description = string.IsNullOrWhiteSpace(achievement.Description) && !unlocked
                        ? "Secret achievement: without description"
                        : achievement.Description,
                    Icon = SteamMediaUrls.AppIconUrl(steamAppId, iconHash),
                    Unlocked = unlockDate,
                    GlobalPercentage = achievement.GlobalPercentage
                }
            });
        }

        return Paginate(FilterAndSortAchievements(rows, name, status, sort, global), page, pageSize);
    }

    private static List<AchievementDto> FilterAndSortAchievements(
        List<AchievementSortRow> rows,
        string? name,
        string? status,
        string? sort,
        string? global)
    {
        var normalizedName = string.IsNullOrWhiteSpace(name) ? null : name.Trim();
        var normalizedStatus = string.IsNullOrWhiteSpace(status)
            ? "locked"
            : status.Trim().ToLowerInvariant();
        if (normalizedStatus is not ("locked" or "unlocked" or "all" or ""))
        {
            normalizedStatus = "locked";
        }

        if (normalizedStatus == "all")
        {
            normalizedStatus = "";
        }

        // If every achievement is unlocked, Locked falls back to showing all.
        if (normalizedStatus == "locked" &&
            rows.Count > 0 &&
            rows.All(r => r.IsUnlocked))
        {
            normalizedStatus = "";
        }

        IEnumerable<AchievementSortRow> query = rows;

        if (normalizedName is not null)
        {
            query = query.Where(r =>
                !string.IsNullOrWhiteSpace(r.Dto.Name) &&
                r.Dto.Name.IndexOf(normalizedName, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        if (normalizedStatus == "unlocked")
        {
            query = query.Where(r => r.IsUnlocked);
        }
        else if (normalizedStatus == "locked")
        {
            query = query.Where(r => !r.IsUnlocked);
        }

        var normalizedGlobal = string.IsNullOrWhiteSpace(global)
            ? ""
            : global.Trim().ToLowerInvariant();
        if (normalizedGlobal is "all")
        {
            normalizedGlobal = "";
        }

        if (normalizedGlobal is "under1" or "under5" or "under10" or "under30" or "under50" or "above50")
        {
            query = query.Where(r => MatchesGlobalPercentageFilter(r.Dto.GlobalPercentage, normalizedGlobal));
        }

        var filtered = query.ToList();
        var normalizedSort = sort?.Trim().ToLowerInvariant() ?? "unlocktime";
        IOrderedEnumerable<AchievementSortRow> ordered = normalizedSort switch
        {
            "name" => filtered.OrderBy(r => r.Dto.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase),
            "global" => filtered
                .OrderBy(r => r.Dto.GlobalPercentage.HasValue ? 0 : 1)
                .ThenBy(r => r.Dto.GlobalPercentage)
                .ThenBy(r => r.Dto.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase),
            _ => filtered
                .OrderByDescending(r => r.IsUnlocked && r.UnlockUtc.HasValue)
                .ThenByDescending(r => r.UnlockUtc)
                .ThenBy(r => r.Dto.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase)
        };

        return ordered.Select(r => r.Dto).ToList();
    }

    private static bool MatchesGlobalPercentageFilter(decimal? percentage, string filter)
    {
        if (percentage is not decimal value)
        {
            return false;
        }

        return filter switch
        {
            "under1" => value < 1m,
            "under5" => value >= 1m && value < 5m,
            "under10" => value >= 5m && value < 10m,
            "under30" => value >= 10m && value < 30m,
            "under50" => value >= 30m && value < 50m,
            "above50" => value >= 50m,
            _ => true
        };
    }

    private sealed class AchievementSortRow
    {
        public required AchievementDto Dto { get; init; }
        public bool IsUnlocked { get; init; }
        public DateTime? UnlockUtc { get; init; }
    }

    private async Task<IReadOnlyList<RecentGameDto>> GetOrFetchRecentGamesAsync(
        string steamId,
        CancellationToken cancellationToken)
    {
        var cacheKey = RecentGamesCacheKeys.ForSteamId(steamId);
        if (_cache.TryGetValue(cacheKey, out IReadOnlyList<RecentGameDto>? cached) && cached is not null)
        {
            return cached;
        }

        var games = await _steamApiClient.GetRecentlyPlayedGamesAsync(steamId, cancellationToken);
        var mapped = new List<RecentGameDto>(games.Count);

        foreach (var game in games)
        {
            mapped.Add(new RecentGameDto
            {
                AppId = game.AppId,
                Name = game.Name,
                PlayTimeWeeks = ToHours(game.Playtime2WeeksMinutes),
                PlayTimeTotal = ToHours(game.PlaytimeForeverMinutes),
                Image = game.ImgIconUrl,
                Achievements = await GetAchievementSummaryAsync(steamId, game.AppId, cancellationToken)
            });
        }

        _cache.Set(
            cacheKey,
            (IReadOnlyList<RecentGameDto>)mapped,
            new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = RecentGamesCacheKeys.AbsoluteExpiration
            });

        return mapped;
    }

    private async Task<PagedResultDto<LibraryGameDto>> GetLibraryFromSteamAsync(
        string steamId,
        int page,
        int pageSize,
        LibraryGameFilterDto? filters,
        CancellationToken cancellationToken)
    {
        var games = await _steamApiClient.GetOwnedGamesAsync(steamId, cancellationToken);
        var mapped = new List<LibraryGameDto>();

        foreach (var game in games)
        {
            if (string.IsNullOrWhiteSpace(game.ImgIconUrl))
            {
                continue;
            }

            mapped.Add(new LibraryGameDto
            {
                AppId = game.AppId,
                Name = game.Name,
                Icon = game.ImgIconUrl,
                Playtime = game.PlaytimeForeverMinutes == 0 ? 0 : ToHours(game.PlaytimeForeverMinutes),
                NotPlayedSince = game.LastPlayedUnix is null or 0
                    ? "0"
                    : FormatDate(game.LastPlayedUnix.Value),
                HasAchievements = game.HasCommunityVisibleStats,
                AchievementsPercentage = 0
            });
        }

        return Paginate(ApplyLibrarySort(ApplyLibraryFilters(mapped, filters), filters), page, pageSize);
    }

    private async Task<PagedResultDto<LibraryGameDto>> GetLibraryFromDbAsync(
        int userId,
        int page,
        int pageSize,
        LibraryGameFilterDto? filters,
        CancellationToken cancellationToken)
    {
        if (pageSize <= 0)
        {
            pageSize = 25;
        }

        var query = _db.UsersGames
            .AsNoTracking()
            .Where(ug => ug.UserId == userId && ug.Game.ImageUrl != null && ug.Game.ImageUrl != "");

        if (filters is not null)
        {
            if (!string.IsNullOrWhiteSpace(filters.Name))
            {
                var name = filters.Name.Trim().ToLowerInvariant();
                query = query.Where(ug => ug.Game.Name.ToLower().Contains(name));
            }

            if (filters.MinHours is double minHours)
            {
                var minMinutes = (int)Math.Ceiling(minHours * 60);
                query = query.Where(ug => ug.PlaytimeMinutes >= minMinutes);
            }

            if (filters.HasAchievements is bool hasAchievements)
            {
                query = query.Where(ug => ug.Game.HasCommunityVisibleStats == hasAchievements);
            }
        }

        var sort = filters?.Sort?.Trim().ToLowerInvariant() ?? "playtime";
        query = sort switch
        {
            "name" => query.OrderBy(ug => ug.Game.Name),
            "achievements" => query
                .OrderByDescending(ug => ug.Game.HasCommunityVisibleStats == true)
                .ThenByDescending(ug => ug.AchievementsPercentage)
                .ThenBy(ug => ug.Game.Name),
            _ => query
                .OrderByDescending(ug => ug.PlaytimeMinutes)
                .ThenBy(ug => ug.Game.Name)
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var lastPage = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
        var currentPage = page <= 0 ? 1 : Math.Min(page, lastPage);
        var skip = (currentPage - 1) * pageSize;

        var rows = await query
            .Skip(skip)
            .Take(pageSize)
            .Select(ug => new
            {
                ug.Game.GameSteamId,
                ug.Game.Name,
                ug.Game.ImageUrl,
                ug.PlaytimeMinutes,
                ug.LastPlayedUnix,
                HasAchievements = ug.Game.HasCommunityVisibleStats == true,
                ug.AchievementsPercentage
            })
            .ToListAsync(cancellationToken);

        var mapped = new List<LibraryGameDto>(rows.Count);
        foreach (var ug in rows)
        {
            if (!int.TryParse(ug.GameSteamId, out var appId))
            {
                continue;
            }

            mapped.Add(new LibraryGameDto
            {
                AppId = appId,
                Name = ug.Name,
                Icon = SteamMediaUrls.AppIconUrl(ug.GameSteamId, ug.ImageUrl),
                Playtime = ug.PlaytimeMinutes == 0 ? 0 : ToHours(ug.PlaytimeMinutes),
                NotPlayedSince = ug.LastPlayedUnix is null or 0
                    ? "0"
                    : FormatDate(ug.LastPlayedUnix.Value),
                HasAchievements = ug.HasAchievements,
                AchievementsPercentage = ug.AchievementsPercentage
            });
        }

        return new PagedResultDto<LibraryGameDto>
        {
            Data = mapped,
            CurrentPage = currentPage,
            LastPage = lastPage,
            PerPage = pageSize,
            TotalCount = totalCount
        };
    }

    private static List<LibraryGameDto> ApplyLibraryFilters(
        IEnumerable<LibraryGameDto> games,
        LibraryGameFilterDto? filters)
    {
        var query = games.AsEnumerable();

        if (filters is null)
        {
            return query.ToList();
        }

        if (!string.IsNullOrWhiteSpace(filters.Name))
        {
            var name = filters.Name.Trim();
            query = query.Where(g =>
                !string.IsNullOrWhiteSpace(g.Name) &&
                g.Name.Contains(name, StringComparison.OrdinalIgnoreCase));
        }

        if (filters.MinHours is double minHours)
        {
            query = query.Where(g => g.Playtime >= minHours);
        }

        if (filters.HasAchievements is bool hasAchievements)
        {
            query = query.Where(g => g.HasAchievements == hasAchievements);
        }

        return query.ToList();
    }

    private static List<LibraryGameDto> ApplyLibrarySort(
        IEnumerable<LibraryGameDto> games,
        LibraryGameFilterDto? filters)
    {
        var sort = filters?.Sort?.Trim().ToLowerInvariant() ?? "playtime";

        return sort switch
        {
            "name" => games
                .OrderBy(g => g.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            "achievements" => games
                .OrderByDescending(g => g.HasAchievements)
                .ThenByDescending(g => g.AchievementsPercentage)
                .ThenBy(g => g.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            _ => games
                .OrderByDescending(g => g.Playtime)
                .ThenBy(g => g.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ToList()
        };
    }

    private async Task<AchievementSummaryDto> GetAchievementSummaryAsync(string steamId, int appId, CancellationToken cancellationToken)
    {
        var result = await _steamApiClient.GetPlayerAchievementsAsync(steamId, appId, cancellationToken);
        if (result is null || !result.Success || result.Achievements.Count == 0)
        {
            return new AchievementSummaryDto();
        }

        var total = result.Achievements.Count;
        var unlocked = result.Achievements.Count(achievement => achievement.Achieved == 1);
        var percentage = total == 0 ? 0 : Math.Round(unlocked / (double)total, 2) * 100;

        return new AchievementSummaryDto
        {
            Unlocked = unlocked,
            Locked = total - unlocked,
            Total = total,
            Percentage = percentage
        };
    }

    private static PagedResultDto<T> Paginate<T>(IReadOnlyList<T> items, int page, int pageSize)
    {
        if (pageSize <= 0)
        {
            pageSize = 25;
        }

        var totalCount = items.Count;
        var lastPage = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
        var currentPage = page <= 0 ? 1 : Math.Min(page, lastPage);
        var skip = (currentPage - 1) * pageSize;

        return new PagedResultDto<T>
        {
            Data = items.Skip(skip).Take(pageSize).ToList(),
            CurrentPage = currentPage,
            LastPage = lastPage,
            PerPage = pageSize,
            TotalCount = totalCount
        };
    }

    private static double ToHours(int minutes) => Math.Round(minutes / 60.0, 1);

    private static string FormatDate(long unixSeconds)
    {
        return ToLocalDateTime(unixSeconds).ToString("dd/MM/yyyy");
    }

    private static string FormatDateTime(long unixSeconds)
    {
        return ToLocalDateTime(unixSeconds).ToString("dd/MM/yyyy HH:mm:ss");
    }

    private static DateTime ToLocalDateTime(long unixSeconds)
    {
        var utc = DateTimeOffset.FromUnixTimeSeconds(unixSeconds).UtcDateTime;
        return TimeZoneInfo.ConvertTimeFromUtc(utc, DateTimeZone);
    }

    private static string FormatDateTimeFromUtc(DateTime storedUtc)
    {
        var utc = storedUtc.Kind == DateTimeKind.Utc
            ? storedUtc
            : DateTime.SpecifyKind(storedUtc, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeFromUtc(utc, DateTimeZone).ToString("dd/MM/yyyy HH:mm:ss");
    }
}
