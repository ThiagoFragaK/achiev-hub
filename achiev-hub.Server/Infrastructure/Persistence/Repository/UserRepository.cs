using achiev_hub.Server.Application.Users.Interfaces;
using achiev_hub.Server.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace achiev_hub.Server.Infrastructure.Persistence.Repository;

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _db;

    public UserRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<User?> GetTrackedByIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        return _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
    }

    public Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        return _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
    }

    public Task<User?> GetBySteamIdAsync(string steamId, CancellationToken cancellationToken = default)
    {
        return _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.SteamId == steamId, cancellationToken);
    }

    public Task<decimal?> GetAveragePercentageAsync(int userId, CancellationToken cancellationToken = default)
    {
        return _db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => (decimal?)u.AvgPercentage)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<DateTimeOffset?> GetGoalsAcknowledgedAtAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        return _db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.GoalsAcknowledgedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
