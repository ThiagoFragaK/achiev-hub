using achiev_hub.Server.DTOs;
using achiev_hub.Server.Models;
using achiev_hub.Server.Repositories.Interfaces;
using achiev_hub.Server.Services.Interfaces;

namespace achiev_hub.Server.Services;

public class GamesService : IGamesService
{
    private static readonly TimeZoneInfo DateTimeZone = TimeZoneInfo.Local;

    private readonly ISteamRepository _steamRepository;

    public GamesService(ISteamRepository steamRepository)
    {
        _steamRepository = steamRepository;
    }

    public async Task<PagedResultDto<RecentGameDto>> GetRecentGamesAsync(string steamId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
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

        return Paginate(mapped, page, pageSize);
    }

    public async Task<PagedResultDto<LibraryGameDto>> GetLibraryAsync(string steamId, int page, int pageSize, CancellationToken cancellationToken = default)
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

        return Paginate(mapped, page, pageSize);
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

    public async Task<PagedResultDto<AchievementDto>> GetAchievementsAsync(string steamId, int appId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var playerResult = await _steamRepository.GetPlayerAchievementsAsync(steamId, appId, cancellationToken);
        var schema = await _steamRepository.GetGameSchemaAsync(appId, cancellationToken);

        if (playerResult is null || !playerResult.Success)
        {
            return Paginate<AchievementDto>([], page, pageSize);
        }

        var schemaByName = (schema?.Achievements ?? [])
            .Where(achievement => !string.IsNullOrWhiteSpace(achievement.Name))
            .GroupBy(achievement => achievement.Name!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        var mapped = new List<AchievementDto>();
        foreach (var playerAchievement in playerResult.Achievements)
        {
            if (string.IsNullOrWhiteSpace(playerAchievement.ApiName) ||
                !schemaByName.TryGetValue(playerAchievement.ApiName, out var schemaAchievement))
            {
                continue;
            }

            var unlocked = playerAchievement.Achieved == 1;
            mapped.Add(new AchievementDto
            {
                Name = schemaAchievement.DisplayName,
                Description = schemaAchievement.Hidden == 1
                    ? "Secret achievement: without description"
                    : schemaAchievement.Description,
                Icon = unlocked ? schemaAchievement.Icon : schemaAchievement.IconGray,
                Unlocked = unlocked ? FormatDateTime(playerAchievement.UnlockTimeUnix) : "-"
            });
        }

        return Paginate(mapped, page, pageSize);
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
}
