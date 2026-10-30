using achiev_hub.Server.Application.Auth.Interfaces;
using achiev_hub.Server.Application.Common.Interfaces;
using achiev_hub.Server.Domain.Entities;
using achiev_hub.Server.Domain.Enums;
using achiev_hub.Server.Infrastructure.Auth;

namespace achiev_hub.Server.Application.Auth;

public class AuthenticationService : IAuthenticationService
{
    private readonly IRepository<User> _users;
    private readonly JwtTokenService _jwtTokenService;

    public AuthenticationService(
        IRepository<User> users,
        JwtTokenService jwtTokenService)
    {
        _users = users;
        _jwtTokenService = jwtTokenService;
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

        return ToLoginResponse(user);
    }

    public async Task<object> LoginWithVerifiedSteamIdAsync(string steamId, CancellationToken cancellationToken = default)
    {
        var normalizedSteamId = steamId.Trim();
        var user = await _users.FirstOrDefaultAsync(
            u => u.SteamId == normalizedSteamId,
            trackChanges: true,
            cancellationToken);

        if (user is null)
        {
            return AuthResult.Fail("No account is linked to this Steam ID. Please register first.", 404);
        }

        if (user.Status == (int)StatusEnum.Inactive)
        {
            return AuthResult.Fail("Account is inactive", 403);
        }

        user.LastLogin = DateTime.UtcNow;
        await _users.SaveChangesAsync(cancellationToken);
        return ToLoginResponse(user);
    }

    public async Task<object> LinkVerifiedSteamIdAsync(int userId, string steamId, CancellationToken cancellationToken = default)
    {
        var normalizedSteamId = steamId.Trim();
        var user = await _users.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return AuthResult.Fail("User not found", 404);
        }

        var owner = await _users.FirstOrDefaultAsync(
            u => u.SteamId == normalizedSteamId,
            trackChanges: false,
            cancellationToken);

        if (owner is not null && owner.Id != userId)
        {
            return AuthResult.Fail("This Steam account is already linked to another user.", 409);
        }

        user.SteamId = normalizedSteamId;
        user.TokenVersion++;
        await _users.SaveChangesAsync(cancellationToken);
        return ToLoginResponse(user);
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
                ShowGoals = false
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

    private LoginResponseDto ToLoginResponse(User user) => new()
    {
        AccessToken = _jwtTokenService.GenerateToken(user),
        TokenType = "Bearer",
        User = new AuthUserDto
        {
            Id = user.Id,
            Email = user.Email,
            SteamId = user.SteamId,
            Role = user.Role,
            ShowGoals = user.ShowGoals
        }
    };
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
