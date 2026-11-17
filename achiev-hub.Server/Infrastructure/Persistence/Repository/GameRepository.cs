using achiev_hub.Server.Application.Games.Interfaces;
using achiev_hub.Server.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace achiev_hub.Server.Infrastructure.Persistence.Repository;

public class GameRepository : IGameRepository
{
    private readonly ApplicationDbContext _db;

    public GameRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<int?> GetIdBySteamIdAsync(string steamId, CancellationToken cancellationToken = default)
    {
        return _db.Games
            .AsNoTracking()
            .Where(g => g.GameSteamId == steamId)
            .Select(g => (int?)g.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<Game?> GetTrackedBySteamIdAsync(string steamId, CancellationToken cancellationToken = default)
    {
        return _db.Games.FirstOrDefaultAsync(g => g.GameSteamId == steamId, cancellationToken);
    }

    public async Task<IReadOnlyList<Game>> GetTrackedBySteamIdsAsync(
        IReadOnlyCollection<string> steamIds,
        CancellationToken cancellationToken = default)
    {
        const int chunkSize = 500;
        var list = steamIds as IList<string> ?? steamIds.ToList();
        var result = new List<Game>();

        for (var i = 0; i < list.Count; i += chunkSize)
        {
            var chunk = list.Skip(i).Take(chunkSize).ToList();
            var rows = await _db.Games
                .Where(g => chunk.Contains(g.GameSteamId))
                .ToListAsync(cancellationToken);
            result.AddRange(rows);
        }

        return result;
    }

    public void Add(Game game)
    {
        _db.Games.Add(game);
    }
}
