using achiev_hub.Server.DTOs.Auth;
using achiev_hub.Server.Entities;
using achiev_hub.Server.Enums;
using achiev_hub.Server.Repositories.Interfaces;
using achiev_hub.Server.Services.Interfaces;
using achiev_hub.Server.Support;

namespace achiev_hub.Server.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly IRepository<User> _users;
    private readonly JwtTokenService _jwtTokenService;
    private readonly ISyncJobEnqueueService _syncJobEnqueueService;
    private readonly ISteamVisibilityService _steamVisibilityService;
    private readonly ILogger<AuthenticationService> _logger;

    public AuthenticationService(
        IRepository<User> users,
        JwtTokenService jwtTokenService,
        ISyncJobEnqueueService syncJobEnqueueService,
        ISteamVisibilityService steamVisibilityService,
        ILogger<AuthenticationService> logger)
    {
        _users = users;
        _jwtTokenService = jwtTokenService;
        _syncJobEnqueueService = syncJobEnqueueService;
        _steamVisibilityService = steamVisibilityService;
        _logger = logger;
    }

    public async Task<object> LoginAsync(string steamId, string password, CancellationToken cancellationToken = default)
    {
        var normalizedSteamId = steamId.Trim();
        var user = await _users.FirstOrDefaultAsync(
            u => u.SteamId == normalizedSteamId,
            trackChanges: true,
            cancellationToken);

        if (user is null || !BCrypt.Net.BCrypt.Verify(password, user.Password))
        {
            return AuthResult.Fail("Invalid credentials", 401);
        }

        if (user.Status == (int)StatusEnum.Inactive)
        {
            return AuthResult.Fail("Account is inactive", 403);
        }

        if (user.Status == (int)StatusEnum.Provisioning)
        {
            return AuthResult.Fail(
                "Account is still preparing your Steam library. Please wait a moment and try again.",
                403);
        }

        user.LastLogin = DateTime.UtcNow;
        await _users.SaveChangesAsync(cancellationToken);

        var syncEnqueued = false;
        string? syncMessage = null;

        if (!string.IsNullOrWhiteSpace(user.SteamId))
        {
            try
            {
                var isPublic = await _steamVisibilityService.RefreshUserSteamVisibilityAsync(
                    user.Id,
                    user.SteamId,
                    cancellationToken);

                // Reload flag after refresh (tracked entity may already be updated).
                if (isPublic)
                {
                    await _syncJobEnqueueService.EnqueueLoginSyncAsync(user.Id, user.SteamId, cancellationToken);
                    syncEnqueued = true;
                }
                else
                {
                    syncMessage =
                        "Your Steam profile is private. Library sync is paused until game details are public.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to refresh visibility / enqueue login sync for user {UserId}", user.Id);
            }
        }

        return new LoginResponseDto
        {
            AccessToken = _jwtTokenService.GenerateToken(user),
            TokenType = "Bearer",
            SyncEnqueued = syncEnqueued,
            SyncMessage = syncMessage,
            User = new AuthUserDto
            {
                Id = user.Id,
                Email = user.Email,
                SteamId = user.SteamId,
                Role = user.Role,
                SteamLibraryPublic = user.SteamLibraryPublic
            }
        };
    }

    public object ContinueAsGuest(string steamId)
    {
        var normalizedSteamId = steamId.Trim();
        return new LoginResponseDto
        {
            AccessToken = _jwtTokenService.GenerateGuestToken(normalizedSteamId),
            TokenType = "Bearer",
            User = new AuthUserDto
            {
                Id = 0,
                Email = string.Empty,
                SteamId = normalizedSteamId,
                Role = JwtTokenService.GuestRole,
                SteamLibraryPublic = true
            }
        };
    }

    public async Task LogoutAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(userId, cancellationToken);
        if (user is not null)
        {
            user.TokenVersion++;
            await _users.SaveChangesAsync(cancellationToken);
        }
    }
}

public static class AuthResult
{
    public static AuthError Fail(string message, int httpStatus) => new(message, httpStatus);

    public static bool IsError(object result, out AuthError error)
    {
        if (result is AuthError authError)
        {
            error = authError;
            return true;
        }

        error = null!;
        return false;
    }
}

public sealed record AuthError(string Message, int HttpStatus);
