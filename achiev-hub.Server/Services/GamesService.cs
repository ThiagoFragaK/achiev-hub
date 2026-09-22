using achiev_hub.Server.Data;
using achiev_hub.Server.DTOs;
using achiev_hub.Server.Exceptions;
using achiev_hub.Server.Repositories.Interfaces;
using achiev_hub.Server.Services.Interfaces;
using achiev_hub.Server.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace achiev_hub.Server.Services;

public class GamesService : IGamesService
{
    private static readonly TimeZoneInfo DateTimeZone = TimeZoneInfo.Local;

    private readonly ISteamRepository _steamRepository;
    private readonly ApplicationDbContext _db;
    private readonly IMemoryCache _cache;

    public GamesService(ISteamRepository steamRepository, ApplicationDbContext db, IMemoryCache cache)
    {
        _steamRepository = steamRepository;
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
        var storeGame = await _steamRepository.GetStoreGameDetailsAsync(appId, cancellationToken);
        if (storeGame is null)
        {
            return null;
        }

        return new GameDetailsDto
        {
            GameName = storeGame.Name,
            GameImage = storeGame.HeaderImage,
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
        int? userId = null,
        CancellationToken cancellationToken = default)
    {
        if (userId is int authUserId)
        {
            return await GetAchievementsFromDbAsync(authUserId, appId, page, pageSize, name, status, cancellationToken);
        }

        var playerResult = await _steamRepository.GetPlayerAchievementsAsync(steamId, appId, cancellationToken);
        var schema = await _steamRepository.GetGameSchemaAsync(appId, cancellationToken);

        if (playerResult is null || !playerResult.Success)
        {
            throw new SteamApiException();
        }

        var schemaByName = (schema?.Achievements ?? [])
            .Where(achievement => !string.IsNullOrWhiteSpace(achievement.Name))
            .GroupBy(achievement => achievement.Name!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        var normalizedName = string.IsNullOrWhiteSpace(name) ? null : name.Trim();
        var normalizedStatus = string.IsNullOrWhiteSpace(status) ? null : status.Trim().ToLowerInvariant();

        var mapped = new List<AchievementDto>();
        foreach (var playerAchievement in playerResult.Achievements)
        {
            if (string.IsNullOrWhiteSpace(playerAchievement.ApiName))
            {
                continue;
            }

            var unlocked = playerAchievement.Achieved == 1;
            if (normalizedStatus == "unlocked" && !unlocked)
            {
                continue;
            }

            if (normalizedStatus == "locked" && unlocked)
            {
                continue;
            }

            schemaByName.TryGetValue(playerAchievement.ApiName, out var schemaAchievement);
            var displayName = string.IsNullOrWhiteSpace(schemaAchievement?.DisplayName)
                ? playerAchievement.ApiName
                : schemaAchievement.DisplayName;

            if (normalizedName is not null &&
                (displayName is null || displayName.IndexOf(normalizedName, StringComparison.OrdinalIgnoreCase) < 0))
            {
                continue;
            }

            mapped.Add(new AchievementDto
            {
                Name = displayName,
                Description = schemaAchievement is null
                    ? null
                    : schemaAchievement.Hidden == 1
                        ? "Secret achievement: without description"
                        : schemaAchievement.Description,
                Icon = unlocked ? schemaAchievement?.Icon : schemaAchievement?.IconGray,
                Unlocked = unlocked ? FormatDateTime(playerAchievement.UnlockTimeUnix) : "-"
            });
        }

        return Paginate(mapped, page, pageSize);
    }

    private async Task<PagedResultDto<AchievementDto>> GetAchievementsFromDbAsync(
        int userId,
        int appId,
        int page,
        int pageSize,
        string? name,
        string? status,
        CancellationToken cancellationToken)
    {
        var steamAppId = appId.ToString();
        var game = await _db.Games
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.GameSteamId == steamAppId, cancellationToken);

        if (game is null)
        {
            return Paginate(Array.Empty<AchievementDto>(), page, pageSize);
        }

        var achievements = await _db.Achievements
            .AsNoTracking()
            .Where(a => a.GameId == game.Id)
            .OrderBy(a => a.Name)
            .ToListAsync(cancellationToken);

        var unlocks = await _db.UsersAchievements
            .AsNoTracking()
            .Where(ua => ua.UserId == userId && ua.GameId == game.Id)
            .ToDictionaryAsync(ua => ua.AchievementId, cancellationToken);

        var normalizedName = string.IsNullOrWhiteSpace(name) ? null : name.Trim();
        var normalizedStatus = string.IsNullOrWhiteSpace(status) ? null : status.Trim().ToLowerInvariant();

        var mapped = new List<AchievementDto>();
        foreach (var achievement in achievements)
        {
            var unlocked = unlocks.ContainsKey(achievement.Id);
            if (normalizedStatus == "unlocked" && !unlocked)
            {
                continue;
            }

            if (normalizedStatus == "locked" && unlocked)
            {
                continue;
            }

            if (normalizedName is not null &&
                achievement.Name.IndexOf(normalizedName, StringComparison.OrdinalIgnoreCase) < 0)
            {
                continue;
            }

            var unlockDate = "-";
            if (unlocked && unlocks.TryGetValue(achievement.Id, out var usersUnlock) && usersUnlock.UnlockDate.HasValue)
            {
                unlockDate = FormatDateTimeFromUtc(usersUnlock.UnlockDate.Value);
            }

            mapped.Add(new AchievementDto
            {
                Name = achievement.Name,
                Description = string.IsNullOrWhiteSpace(achievement.Description) && !unlocked
                    ? "Secret achievement: without description"
                    : achievement.Description,
                Icon = unlocked ? achievement.ImageUrlUnlock : achievement.ImageUrlLock,
                Unlocked = unlockDate
            });
        }

        return Paginate(mapped, page, pageSize);
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

        var games = await _steamRepository.GetRecentlyPlayedGamesAsync(steamId, cancellationToken);
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
        var games = await _steamRepository.GetOwnedGamesAsync(steamId, cancellationToken);
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
                HasAchievements = game.HasCommunityVisibleStats
            });
        }

        return Paginate(ApplyLibraryFilters(mapped, filters), page, pageSize);
    }

    private async Task<PagedResultDto<LibraryGameDto>> GetLibraryFromDbAsync(
        int userId,
        int page,
        int pageSize,
        LibraryGameFilterDto? filters,
        CancellationToken cancellationToken)
    {
        var rows = await _db.UsersGames
            .AsNoTracking()
            .Where(ug => ug.UserId == userId)
            .Include(ug => ug.Game)
            .OrderBy(ug => ug.Game.Name)
            .ToListAsync(cancellationToken);

        var mapped = new List<LibraryGameDto>();
        foreach (var ug in rows)
        {
            if (string.IsNullOrWhiteSpace(ug.Game.ImageUrl) ||
                ug.Game.GameSteamId is null ||
                !int.TryParse(ug.Game.GameSteamId, out var appId))
            {
                continue;
            }

            mapped.Add(new LibraryGameDto
            {
                AppId = appId,
                Name = ug.Game.Name,
                Icon = ug.Game.ImageUrl,
                Playtime = ug.PlaytimeMinutes == 0 ? 0 : ToHours(ug.PlaytimeMinutes),
                NotPlayedSince = ug.LastPlayedUnix is null or 0
                    ? "0"
                    : FormatDate(ug.LastPlayedUnix.Value),
                HasAchievements = ug.Game.HasCommunityVisibleStats == true
            });
        }

        return Paginate(ApplyLibraryFilters(mapped, filters), page, pageSize);
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

    private async Task<AchievementSummaryDto> GetAchievementSummaryAsync(string steamId, int appId, CancellationToken cancellationToken)
    {
        var result = await _steamRepository.GetPlayerAchievementsAsync(steamId, appId, cancellationToken);
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
