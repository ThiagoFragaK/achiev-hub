using achiev_hub.Server.DTOs.Persistence;
using achiev_hub.Server.Entities;
using achiev_hub.Server.Enums;
using achiev_hub.Server.Exceptions;
using achiev_hub.Server.Repositories.Interfaces;
using achiev_hub.Server.Services.Interfaces;

namespace achiev_hub.Server.Services;

public class UserService : IUserService
{
    private readonly IRepository<User> _users;

    public UserService(IRepository<User> users)
    {
        _users = users;
    }

    public async Task<IReadOnlyList<UserDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var users = await _users.GetAllAsync(cancellationToken);
        return users.Select(Map).ToList();
    }

    public async Task<UserDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return Map(await GetRequiredAsync(id, cancellationToken));
    }

    public async Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureEmailIsUniqueAsync(request.Email, null, cancellationToken);
        await EnsureSteamIdIsUniqueAsync(request.SteamId, null, cancellationToken);

        var user = new User
        {
            Email = request.Email.Trim(),
            SteamId = NormalizeSteamId(request.SteamId),
            Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = "user",
            Status = (int)StatusEnum.Active,
            TokenVersion = 0
        };

        await _users.AddAsync(user, cancellationToken);
        await _users.SaveChangesAsync(cancellationToken);
        return Map(user);
    }

    public async Task<UserDto> UpdateAsync(int id, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = await GetRequiredAsync(id, cancellationToken);
        await EnsureEmailIsUniqueAsync(request.Email, id, cancellationToken);
        await EnsureSteamIdIsUniqueAsync(request.SteamId, id, cancellationToken);

        user.Email = request.Email.Trim();
        user.SteamId = NormalizeSteamId(request.SteamId);
        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            user.Password = BCrypt.Net.BCrypt.HashPassword(request.Password);
        }

        _users.Update(user);
        await _users.SaveChangesAsync(cancellationToken);
        return Map(user);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var user = await GetRequiredAsync(id, cancellationToken);
        _users.Remove(user);
        await _users.SaveChangesAsync(cancellationToken);
    }

    private async Task<User> GetRequiredAsync(int id, CancellationToken cancellationToken)
    {
        return await _users.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"User {id} was not found.");
    }

    private async Task EnsureEmailIsUniqueAsync(string email, int? excludeId, CancellationToken cancellationToken)
    {
        var exists = await _users.AnyAsync(
            user => user.Email == email.Trim() && (!excludeId.HasValue || user.Id != excludeId.Value),
            cancellationToken);

        if (exists)
        {
            throw new ConflictException("Email is already in use.");
        }
    }

    private async Task EnsureSteamIdIsUniqueAsync(string? steamId, int? excludeId, CancellationToken cancellationToken)
    {
        var normalized = NormalizeSteamId(steamId);
        if (normalized is null)
        {
            return;
        }

        var exists = await _users.AnyAsync(
            user => user.SteamId == normalized && (!excludeId.HasValue || user.Id != excludeId.Value),
            cancellationToken);

        if (exists)
        {
            throw new ConflictException("Steam ID is already in use.");
        }
    }

    private static string? NormalizeSteamId(string? steamId)
    {
        return string.IsNullOrWhiteSpace(steamId) ? null : steamId.Trim();
    }

    private static UserDto Map(User user) => new()
    {
        Id = user.Id,
        Email = user.Email,
        SteamId = user.SteamId
    };
}
