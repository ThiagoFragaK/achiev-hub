using achiev_hub.Server.DTOs.Auth;
using achiev_hub.Server.Entities;
using achiev_hub.Server.Enums;
using achiev_hub.Server.Repositories.Interfaces;
using achiev_hub.Server.Services;
using achiev_hub.Server.Services.Interfaces;
using achiev_hub.Server.Support;

namespace achiev_hub.Server.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly IRepository<User> _users;
    private readonly JwtTokenService _jwtTokenService;
    private readonly ISteamSyncService _steamSyncService;
    private readonly ILogger<AuthenticationService> _logger;

    public AuthenticationService(
        IRepository<User> users,
        JwtTokenService jwtTokenService,
        ISteamSyncService steamSyncService,
        ILogger<AuthenticationService> logger)
    {
        _users = users;
        _jwtTokenService = jwtTokenService;
        _steamSyncService = steamSyncService;
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

        user.LastLogin = DateTime.UtcNow;
        await _users.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(user.SteamId))
        {
            try
            {
                await _steamSyncService.SyncLibraryAsync(
                    user.Id,
                    user.SteamId,
                    LibrarySyncScope.Recent,
                    cancellationToken);
                await _steamSyncService.SyncAchievementsForUserAsync(
                    user.Id,
                    user.SteamId,
                    AchievementSyncScope.RecentTwoWeeks,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Library sync failed after login for user {UserId}", user.Id);
            }
        }

        return new LoginResponseDto
        {
            AccessToken = _jwtTokenService.GenerateToken(user),
            TokenType = "Bearer",
            User = new AuthUserDto
            {
                Id = user.Id,
                Email = user.Email,
                SteamId = user.SteamId,
                Role = user.Role
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
                Role = JwtTokenService.GuestRole
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
