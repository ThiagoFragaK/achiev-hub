using System.Security.Cryptography;
using System.Text;
using achiev_hub.Server.DTOs.Auth;
using achiev_hub.Server.DTOs.Persistence;
using achiev_hub.Server.Entities;
using achiev_hub.Server.Enums;
using achiev_hub.Server.Repositories.Interfaces;
using achiev_hub.Server.Services;
using achiev_hub.Server.Services.Interfaces;
using achiev_hub.Server.Support;

namespace achiev_hub.Server.Services;

public class RegistrationService : IRegistrationService
{
    public const int ResendCooldownSeconds = 60;
    private static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan ConsumeTokenLifetime = TimeSpan.FromMinutes(15);

    private readonly IRepository<User> _users;
    private readonly IRepository<EmailVerification> _verifications;
    private readonly IEmailSender _emailSender;
    private readonly IHostEnvironment _environment;
    private readonly ISteamSyncService _steamSyncService;
    private readonly ILogger<RegistrationService> _logger;

    public RegistrationService(
        IRepository<User> users,
        IRepository<EmailVerification> verifications,
        IEmailSender emailSender,
        IHostEnvironment environment,
        ISteamSyncService steamSyncService,
        ILogger<RegistrationService> logger)
    {
        _users = users;
        _verifications = verifications;
        _emailSender = emailSender;
        _environment = environment;
        _steamSyncService = steamSyncService;
        _logger = logger;
    }

    public async Task<object> SendVerificationAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(email);
        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            return AuthResult.Fail("Email is required", 422);
        }

        if (await _users.AnyAsync(u => u.Email == normalizedEmail, cancellationToken))
        {
            return AuthResult.Fail("Email is already registered", 409);
        }

        var existing = await _verifications.FirstOrDefaultAsync(
            v => v.Email == normalizedEmail,
            trackChanges: true,
            cancellationToken);

        var now = DateTime.UtcNow;
        if (existing is not null)
        {
            var elapsed = now - existing.LastSentAt;
            if (elapsed < TimeSpan.FromSeconds(ResendCooldownSeconds))
            {
                var remaining = (int)Math.Ceiling(ResendCooldownSeconds - elapsed.TotalSeconds);
                return AuthResult.Fail($"Please wait {remaining} seconds before requesting another code.", 429);
            }
        }

        var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        var codeHash = HashValue(code);

        if (existing is null)
        {
            existing = new EmailVerification { Email = normalizedEmail };
            await _verifications.AddAsync(existing, cancellationToken);
        }

        existing.CodeHash = codeHash;
        existing.ExpiresAt = now.Add(CodeLifetime);
        existing.LastSentAt = now;
        existing.VerifiedAt = null;
        existing.ConsumeTokenHash = null;
        existing.ConsumeTokenExpiresAt = null;

        await _emailSender.SendAsync(
            normalizedEmail,
            "Your Achievements Hub verification code",
            $"Your verification code is {code}. It expires in 15 minutes.",
            cancellationToken);

        await _verifications.SaveChangesAsync(cancellationToken);

        return new { success = true, message = "Verification code sent", cooldownSeconds = ResendCooldownSeconds };
    }

    public async Task<object> ConfirmCodeAsync(string email, string code, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(email);
        if (string.IsNullOrWhiteSpace(normalizedEmail) || string.IsNullOrWhiteSpace(code))
        {
            return AuthResult.Fail("Email and code are required", 422);
        }

        var verification = await _verifications.FirstOrDefaultAsync(
            v => v.Email == normalizedEmail,
            trackChanges: true,
            cancellationToken);

        if (verification is null || verification.ExpiresAt < DateTime.UtcNow)
        {
            return AuthResult.Fail("Verification code is invalid or expired", 400);
        }

        if (!SecureEquals(verification.CodeHash, HashValue(code.Trim())))
        {
            return AuthResult.Fail("Verification code is invalid or expired", 400);
        }

        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        verification.VerifiedAt = DateTime.UtcNow;
        verification.ConsumeTokenHash = HashValue(token);
        verification.ConsumeTokenExpiresAt = DateTime.UtcNow.Add(ConsumeTokenLifetime);
        verification.CodeHash = string.Empty;

        await _verifications.SaveChangesAsync(cancellationToken);

        return new ConfirmCodeResponseDto { EmailVerifiedToken = token };
    }

    public async Task<object> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(request.Email);
        var steamId = NormalizeSteamId(request.SteamId);
        var bypassEmailVerification = _environment.IsDevelopment();

        if (string.IsNullOrWhiteSpace(normalizedEmail)
            || string.IsNullOrWhiteSpace(steamId)
            || string.IsNullOrWhiteSpace(request.Password)
            || (!bypassEmailVerification && string.IsNullOrWhiteSpace(request.EmailVerifiedToken)))
        {
            return AuthResult.Fail(
                bypassEmailVerification
                    ? "Steam ID, email, and password are required"
                    : "Steam ID, email, password, and verified email token are required",
                422);
        }

        if (!PasswordValidator.IsValid(request.Password, out var passwordError))
        {
            return AuthResult.Fail(passwordError, 422);
        }

        if (await _users.AnyAsync(u => u.Email == normalizedEmail, cancellationToken))
        {
            return AuthResult.Fail("Email is already registered", 409);
        }

        if (await _users.AnyAsync(u => u.SteamId == steamId, cancellationToken))
        {
            return AuthResult.Fail("Steam ID is already registered", 409);
        }

        EmailVerification? verification = null;
        if (!bypassEmailVerification)
        {
            verification = await _verifications.FirstOrDefaultAsync(
                v => v.Email == normalizedEmail,
                trackChanges: true,
                cancellationToken);

            if (verification is null
                || verification.VerifiedAt is null
                || verification.ConsumeTokenHash is null
                || verification.ConsumeTokenExpiresAt is null
                || verification.ConsumeTokenExpiresAt < DateTime.UtcNow
                || !SecureEquals(verification.ConsumeTokenHash, HashValue(request.EmailVerifiedToken)))
            {
                return AuthResult.Fail("Email has not been verified", 400);
            }
        }

        var user = new User
        {
            Email = normalizedEmail,
            SteamId = steamId,
            Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = "user",
            Status = (int)StatusEnum.Active,
            IsEmailVerified = !bypassEmailVerification,
            TokenVersion = 0
        };

        await _users.AddAsync(user, cancellationToken);
        if (verification is not null)
        {
            _verifications.Remove(verification);
        }

        await _users.SaveChangesAsync(cancellationToken);

        try
        {
            await _steamSyncService.SyncLibraryAsync(
                user.Id,
                user.SteamId,
                LibrarySyncScope.Full,
                cancellationToken);
            await _steamSyncService.SyncAchievementsForUserAsync(
                user.Id,
                user.SteamId,
                AchievementSyncScope.AllOwnedWithStats,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Initial library/achievement sync failed for user {UserId}", user.Id);
        }

        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            SteamId = user.SteamId
        };
    }

    private static string NormalizeEmail(string? email) =>
        string.IsNullOrWhiteSpace(email) ? string.Empty : email.Trim().ToLowerInvariant();

    private static string? NormalizeSteamId(string? steamId) =>
        string.IsNullOrWhiteSpace(steamId) ? null : steamId.Trim();

    private static string HashValue(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes);
    }

    private static bool SecureEquals(string a, string b)
    {
        var aBytes = Encoding.UTF8.GetBytes(a);
        var bBytes = Encoding.UTF8.GetBytes(b);
        return aBytes.Length == bBytes.Length && CryptographicOperations.FixedTimeEquals(aBytes, bBytes);
    }
}
