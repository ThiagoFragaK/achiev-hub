using achiev_hub.Server.Domain.Entities;

namespace achiev_hub.Server.Application.Steam.Interfaces;

public interface IUserSyncStatusRepository
{
    Task<UserSyncStatus?> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
}
