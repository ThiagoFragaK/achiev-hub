using achiev_hub.Server.Application.Steam.Interfaces;
using achiev_hub.Server.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace achiev_hub.Server.Infrastructure.Persistence.Repository;

public class UserSyncStatusRepository : IUserSyncStatusRepository
{
    private readonly ApplicationDbContext _db;

    public UserSyncStatusRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<UserSyncStatus?> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        return _db.UserSyncStatuses
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);
    }
}
