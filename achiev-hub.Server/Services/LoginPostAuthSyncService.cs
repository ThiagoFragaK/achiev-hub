using achiev_hub.Server.Data;
using achiev_hub.Server.Enums;
using achiev_hub.Server.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace achiev_hub.Server.Services;

/// <summary>Post-login fire-and-forget: visibility refresh + smart sync enqueue.</summary>
public class LoginPostAuthSyncService : ILoginPostAuthSyncService
{
    private readonly ApplicationDbContext _db;
    private readonly ISteamVisibilityService _steamVisibilityService;
    private readonly ISyncJobEnqueueService _syncJobEnqueueService;
    private readonly ILogger<LoginPostAuthSyncService> _logger;

    public LoginPostAuthSyncService(
        ApplicationDbContext db,
        ISteamVisibilityService steamVisibilityService,
        ISyncJobEnqueueService syncJobEnqueueService,
        ILogger<LoginPostAuthSyncService> logger)
    {
        _db = db;
        _steamVisibilityService = steamVisibilityService;
        _syncJobEnqueueService = syncJobEnqueueService;
        _logger = logger;
    }

    public async Task RunAsync(int userId, string steamId, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
            if (user is null)
            {
                return;
            }

            // Legacy Provisioning accounts become Active as soon as they can log in.
            if (user.Status == (int)StatusEnum.Provisioning)
            {
                user.Status = (int)StatusEnum.Active;
                await _db.SaveChangesAsync(cancellationToken);
            }

            var isPublic = await _steamVisibilityService.RefreshUserSteamVisibilityAsync(
                userId,
                steamId,
                cancellationToken);

            if (!isPublic)
            {
                _logger.LogInformation(
                    "Skipping login sync for user {UserId}: Steam library private",
                    userId);
                // #region agent log
                try { System.IO.File.AppendAllText(@"K:\Projekten\MyApps\achiev-hub\debug-321fb6.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "321fb6", runId = "pre-fix", hypothesisId = "A", location = "LoginPostAuthSyncService.cs:private", message = "Login sync skipped: Steam private", data = new { userId }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
                // #endregion
                return;
            }

            var jobIds = await _syncJobEnqueueService.EnqueueLoginSyncAsync(
                userId,
                steamId,
                cancellationToken);

            _logger.LogInformation(
                "Login post-auth sync user={UserId} jobs={Count}",
                userId,
                jobIds.Count);
            // #region agent log
            try { System.IO.File.AppendAllText(@"K:\Projekten\MyApps\achiev-hub\debug-321fb6.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "321fb6", runId = "pre-fix", hypothesisId = "A", location = "LoginPostAuthSyncService.cs:enqueued", message = "Login post-auth enqueue result", data = new { userId, isPublic, jobCount = jobIds.Count, jobIds }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
            // #endregion
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Login post-auth sync failed for user {UserId}", userId);
        }
    }
}
