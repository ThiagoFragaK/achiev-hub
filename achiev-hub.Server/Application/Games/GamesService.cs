using achiev_hub.Server.Application.Achievements;
using achiev_hub.Server.Application.Achievements.Interfaces;
using achiev_hub.Server.Application.Common;
using achiev_hub.Server.Application.Games.Interfaces;
using achiev_hub.Server.Application.Steam;
using achiev_hub.Server.Application.Steam.Interfaces;
using achiev_hub.Server.Application.Users.Interfaces;
using achiev_hub.Server.Infrastructure.Auth;
using Microsoft.Extensions.Caching.Memory;

namespace achiev_hub.Server.Application.Games;

public class GamesService : IGamesService
{
    private static readonly TimeZoneInfo DateTimeZone = TimeZoneInfo.Local;

    private readonly ISteamApiClient _steamApiClient;
    private readonly IUsersGameRepository _usersGames;
    private readonly IUsersAchievementRepository _usersAchievements;
    private readonly IGameRepository _games;
    private readonly IAchievementRepository _achievements;
    private readonly IMemoryCache _cache;

    public GamesService(
        ISteamApiClient steamApiClient,
        IUsersGameRepository usersGames,
        IUsersAchievementRepository usersAchievements,
        IGameRepository games,
        IAchievementRepository achievements,
        IMemoryCache cache)
    {
        _steamApiClient = steamApiClient;
        _usersGames = usersGames;
        _usersAchievements = usersAchievements;
        _games = games;
        _achievements = achievements;
        _cache = cache;
    }

    public async Task<PagedResultDto<RecentGameDto>> GetRecentGamesAsync(
        string steamId,
        int page,
        int pageSize,
        int? userId = null,
        CancellationToken cancellationToken = default)
    {
        // Recent games always come from Steam, with a short memory cache.
        _ = userId;
        var mapped = await GetOrFetchRecentGamesAsync(steamId, cancellationToken);
        return PaginationHelper.Paginate(mapped, page, pageSize);
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

        return PaginationHelper.Paginate(FilterAndSortAchievements(rows, name, status, sort, global), page, pageSize);
    }

    public async Task<PagedResultDto<RecentAchievementDto>> GetRecentAchievementsAsync(
        int userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        const int maxRecent = 100;

        var pageResult = await _usersAchievements.GetRecentUnlockedPageAsync(
            userId,
            page,
            pageSize,
            maxRecent,
            cancellationToken);

        var data = pageResult.Data.Select(row =>
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

        return PagedResultDto<RecentAchievementDto>.From(data, pageResult.Pagination);
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
        var gameId = await _games.GetIdBySteamIdAsync(steamAppId, cancellationToken);

        if (gameId is not int id)
        {
            return PaginationHelper.Paginate(Array.Empty<AchievementDto>(), page, pageSize);
        }

        var unlocks = await _usersAchievements.GetUnlockDatesByAchievementIdAsync(
            userId,
            id,
            cancellationToken);
        var catalog = await _achievements.GetCatalogAsync(id, cancellationToken);

        var rows = new List<AchievementSortRow>(catalog.Count);
        foreach (var achievement in catalog)
        {
            var unlocked = unlocks.TryGetValue(achievement.Id, out var unlockDateValue);
            DateTime? unlockUtc = null;
            var unlockDate = "-";
            if (unlocked && unlockDateValue.HasValue)
            {
                unlockUtc = DateTime.SpecifyKind(unlockDateValue.Value, DateTimeKind.Utc);
                unlockDate = FormatDateTimeFromUtc(unlockDateValue.Value);
            }

            var iconHash = unlocked ? achievement.ImageUrlUnlock : achievement.ImageUrlLock;
            rows.Add(new AchievementSortRow
            {
                IsUnlocked = unlocked,
                UnlockUtc = unlockUtc,
                Dto = new AchievementDto
                {
                    Id = achievement.Id,
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

        return PaginationHelper.Paginate(FilterAndSortAchievements(rows, name, status, sort, global), page, pageSize);
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
                (!string.IsNullOrWhiteSpace(r.Dto.Name) &&
                 r.Dto.Name.IndexOf(normalizedName, StringComparison.OrdinalIgnoreCase) >= 0)
                || (!string.IsNullOrWhiteSpace(r.Dto.Description) &&
                    r.Dto.Description.IndexOf(normalizedName, StringComparison.OrdinalIgnoreCase) >= 0));
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
        var games = await _steamApiClient.GetOwnedGamesAsync(steamId, cancellationToken: cancellationToken);
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

        return PaginationHelper.Paginate(ApplyLibrarySort(ApplyLibraryFilters(mapped, filters), filters), page, pageSize);
    }

    private async Task<PagedResultDto<LibraryGameDto>> GetLibraryFromDbAsync(
        int userId,
        int page,
        int pageSize,
        LibraryGameFilterDto? filters,
        CancellationToken cancellationToken)
    {
        var pageResult = await _usersGames.GetLibraryPageAsync(
            userId,
            new LibraryGameQuery
            {
                Name = filters?.Name,
                MinHours = filters?.MinHours,
                HasAchievements = filters?.HasAchievements,
                Sort = filters?.Sort,
                Page = page,
                PageSize = pageSize
            },
            cancellationToken);

        var mapped = new List<LibraryGameDto>(pageResult.Data.Count);
        foreach (var ug in pageResult.Data)
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

        return PagedResultDto<LibraryGameDto>.From(mapped, pageResult.Pagination);
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
