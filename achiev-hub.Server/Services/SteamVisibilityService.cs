using achiev_hub.Server.Data;
using achiev_hub.Server.Repositories.Interfaces;
using achiev_hub.Server.Services.Interfaces;
using achiev_hub.Server.Support;
using Microsoft.EntityFrameworkCore;

namespace achiev_hub.Server.Services;

public class SteamVisibilityService : ISteamVisibilityService
{
    private readonly ApplicationDbContext _db;
    private readonly ISteamRepository _steamRepository;
    private readonly ILogger<SteamVisibilityService> _logger;

    public SteamVisibilityService(
        ApplicationDbContext db,
        ISteamRepository steamRepository,
        ILogger<SteamVisibilityService> logger)
    {
        _db = db;
        _steamRepository = steamRepository;
        _logger = logger;
    }

    public async Task<(bool IsLibraryPublic, string? PersonaName, string? Avatar)> ProbeAsync(
        string steamId,
        CancellationToken cancellationToken = default)
    {
        var player = await _steamRepository.GetPlayerBySteamIdAsync(steamId, cancellationToken);
        if (player is null || string.IsNullOrWhiteSpace(player.SteamId))
        {
            return (false, null, null);
        }

        return (
            SteamVisibility.IsLibraryPublic(player),
            player.PersonaName,
            player.AvatarFull ?? player.Avatar);
    }

    public async Task<bool> RefreshUserSteamVisibilityAsync(
        int userId,
        string steamId,
        CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
        {
            return false;
        }

        var player = await _steamRepository.GetPlayerBySteamIdAsync(steamId, cancellationToken);
        if (player is null || string.IsNullOrWhiteSpace(player.SteamId))
        {
            _logger.LogWarning(
                "Could not refresh Steam visibility for user {UserId}: profile missing",
                userId);
            user.SteamLibraryPublic = false;
            await _db.SaveChangesAsync(cancellationToken);
            return false;
        }

        var isPublic = SteamVisibility.IsLibraryPublic(player);
        if (user.SteamLibraryPublic != isPublic)
        {
            _logger.LogInformation(
                "User {UserId} SteamLibraryPublic changed to {IsPublic}",
                userId,
                isPublic);
        }

        user.SteamLibraryPublic = isPublic;

        if (isPublic)
        {
            // Allow previously unavailable games to be retried after privacy was opened.
            await _db.UsersGames
                .Where(ug => ug.UserId == userId && ug.AchievementSyncUnavailable)
                .ExecuteUpdateAsync(
                    s => s
                        .SetProperty(ug => ug.AchievementSyncUnavailable, false)
                        .SetProperty(ug => ug.NeedsAchievementRefresh, true),
                    cancellationToken);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return isPublic;
    }
}
