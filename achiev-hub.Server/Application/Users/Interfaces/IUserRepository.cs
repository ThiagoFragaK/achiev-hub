using achiev_hub.Server.Domain.Entities;

namespace achiev_hub.Server.Application.Users.Interfaces;

public interface IUserRepository
{
    Task<User?> GetTrackedByIdAsync(int userId, CancellationToken cancellationToken = default);

    Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken = default);

    Task<User?> GetBySteamIdAsync(string steamId, CancellationToken cancellationToken = default);

    Task<decimal?> GetAveragePercentageAsync(int userId, CancellationToken cancellationToken = default);

    Task<DateTimeOffset?> GetGoalsAcknowledgedAtAsync(int userId, CancellationToken cancellationToken = default);
}
