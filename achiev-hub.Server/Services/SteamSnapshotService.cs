using achiev_hub.Server.Data;
using achiev_hub.Server.Entities;
using achiev_hub.Server.Models;
using achiev_hub.Server.Repositories.Interfaces;
using achiev_hub.Server.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using SteamSync.Shared;

namespace achiev_hub.Server.Services;

/// <summary>
/// Lightweight Steam snapshot at registration (max 3 API calls, 5s each).
/// Does not sync achievements — only profile shell + owned/recent playtime rows.
/// </summary>
public class SteamSnapshotService : ISteamSnapshotService
{
    private static readonly TimeSpan CallTimeout = TimeSpan.FromSeconds(5);

    private readonly ApplicationDbContext _db;
    private readonly ISteamRepository _steamRepository;
    private readonly ILogger<SteamSnapshotService> _logger;

    public SteamSnapshotService(
        ApplicationDbContext db,
        ISteamRepository steamRepository,
        ILogger<SteamSnapshotService> logger)
    {
        _db = db;
        _steamRepository = steamRepository;
        _logger = logger;
    }

    public async Task PersistRegistrationSnapshotAsync(
        int userId,
        string steamId,
        CancellationToken cancellationToken = default)
    {
        var owned = await CallWithTimeoutAsync(
            ct => _steamRepository.GetOwnedGamesAsync(steamId, ct),
            [],
            "GetOwnedGames",
            steamId,
            cancellationToken);

        var recent = await CallWithTimeoutAsync(
            ct => _steamRepository.GetRecentlyPlayedGamesAsync(steamId, ct),
            [],
            "GetRecentlyPlayedGames",
            steamId,
            cancellationToken);

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
        {
            return;
        }

        user.Playtime2WeeksMinutes = recent.Sum(g => g.Playtime2WeeksMinutes);

        var byAppId = new Dictionary<int, SnapshotGame>();
        foreach (var game in owned)
        {
            if (game.AppId <= 0)
            {
                continue;
            }

            byAppId[game.AppId] = new SnapshotGame(
                game.AppId,
                game.Name,
                game.ImgIconUrl,
                game.PlaytimeForeverMinutes,
                game.LastPlayedUnix,
                game.HasCommunityVisibleStats);
        }

        foreach (var game in recent)
        {
            if (game.AppId <= 0)
            {
                continue;
            }

            if (byAppId.TryGetValue(game.AppId, out var existing))
            {
                byAppId[game.AppId] = existing with
                {
                    Name = string.IsNullOrWhiteSpace(existing.Name) ? game.Name : existing.Name,
                    ImageUrl = string.IsNullOrWhiteSpace(existing.ImageUrl) ? game.ImgIconUrl : existing.ImageUrl,
                    PlaytimeMinutes = existing.PlaytimeMinutes > 0
                        ? existing.PlaytimeMinutes
                        : game.PlaytimeForeverMinutes
                };
            }
            else
            {
                byAppId[game.AppId] = new SnapshotGame(
                    game.AppId,
                    game.Name,
                    game.ImgIconUrl,
                    game.PlaytimeForeverMinutes,
                    null,
                    null);
            }
        }

        if (byAppId.Count > 0)
        {
            var steamIds = byAppId.Keys.Select(id => id.ToString()).ToList();
            var existingGames = await _db.Games
                .Where(g => g.GameSteamId != null && steamIds.Contains(g.GameSteamId))
                .ToListAsync(cancellationToken);
            var gamesBySteamId = existingGames
                .Where(g => g.GameSteamId is not null)
                .ToDictionary(g => g.GameSteamId!, StringComparer.Ordinal);

            foreach (var input in byAppId.Values)
            {
                var steamAppId = input.AppId.ToString();
                if (!gamesBySteamId.TryGetValue(steamAppId, out var game))
                {
                    game = new Game
                    {
                        GameSteamId = steamAppId,
                        Name = string.IsNullOrWhiteSpace(input.Name) ? steamAppId : input.Name.Trim(),
                        ImageUrl = input.ImageUrl,
                        HasCommunityVisibleStats = input.HasCommunityVisibleStats
                    };
                    _db.Games.Add(game);
                    gamesBySteamId[steamAppId] = game;
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(input.Name))
                    {
                        game.Name = input.Name.Trim();
                    }

                    if (!string.IsNullOrWhiteSpace(input.ImageUrl))
                    {
                        game.ImageUrl = input.ImageUrl;
                    }

                    if (input.HasCommunityVisibleStats.HasValue)
                    {
                        game.HasCommunityVisibleStats = input.HasCommunityVisibleStats;
                    }
                }
            }

            await _db.SaveChangesAsync(cancellationToken);

            var gameIds = gamesBySteamId.Values.Select(g => g.Id).ToList();
            var existingLinks = await _db.UsersGames
                .Where(ug => ug.UserId == userId && gameIds.Contains(ug.GameId))
                .ToListAsync(cancellationToken);
            var linksByGameId = existingLinks.ToDictionary(ug => ug.GameId);

            foreach (var input in byAppId.Values)
            {
                var game = gamesBySteamId[input.AppId.ToString()];
                if (!linksByGameId.TryGetValue(game.Id, out var link))
                {
                    link = new UsersGame
                    {
                        UserId = userId,
                        GameId = game.Id
                    };
                    _db.UsersGames.Add(link);
                    linksByGameId[game.Id] = link;
                }

                link.PlaytimeMinutes = input.PlaytimeMinutes;
                if (input.LastPlayedUnix.HasValue)
                {
                    link.LastPlayedUnix = input.LastPlayedUnix;
                }
            }
        }

        var status = await _db.UserSyncStatuses.FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);
        if (status is null)
        {
            status = new UserSyncStatus { UserId = userId };
            _db.UserSyncStatuses.Add(status);
        }

        status.Status = SyncStatus.Pending;
        status.TotalGamesCount = byAppId.Count;
        status.GamesSyncedCount = 0;
        status.SyncProgressPercent = 0;
        status.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation(
            "Registration snapshot user={UserId} owned={Owned} recent={Recent}",
            userId,
            owned.Count,
            recent.Count);
    }

    private async Task<T> CallWithTimeoutAsync<T>(
        Func<CancellationToken, Task<T>> call,
        T fallback,
        string label,
        string steamId,
        CancellationToken cancellationToken)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(CallTimeout);
            return await call(cts.Token);
        }
        catch (Exception ex) when (ex is OperationCanceledException or TaskCanceledException or HttpRequestException)
        {
            _logger.LogWarning(ex, "Steam snapshot {Label} timed out/failed for {SteamId}", label, steamId);
            return fallback;
        }
    }

    private sealed record SnapshotGame(
        int AppId,
        string? Name,
        string? ImageUrl,
        int PlaytimeMinutes,
        long? LastPlayedUnix,
        bool? HasCommunityVisibleStats);
}
