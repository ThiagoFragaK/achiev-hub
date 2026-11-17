using achiev_hub.Server.Domain.Entities;

namespace achiev_hub.Server.Application.Games.Interfaces;

public interface IGameRepository
{
    Task<int?> GetIdBySteamIdAsync(string steamId, CancellationToken cancellationToken = default);

    Task<Game?> GetTrackedBySteamIdAsync(string steamId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Game>> GetTrackedBySteamIdsAsync(
        IReadOnlyCollection<string> steamIds,
        CancellationToken cancellationToken = default);

    void Add(Game game);
}
