using achiev_hub.Server.Domain.Entities;

namespace achiev_hub.Server.Application.Achievements.Interfaces;

public interface IAchievementRepository
{
    Task<int> CountForGameAsync(int gameId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AchievementIdentityRow>> GetIdentityRowsAsync(
        IReadOnlyCollection<int> achievementIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AchievementCatalogRow>> GetCatalogAsync(
        int gameId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Achievement>> GetTrackedByGameIdAsync(
        int gameId,
        CancellationToken cancellationToken = default);

    void Add(Achievement achievement);
}

public sealed class AchievementIdentityRow
{
    public int Id { get; set; }
    public int GameId { get; set; }
}

public sealed class AchievementCatalogRow
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrlLock { get; set; }
    public string? ImageUrlUnlock { get; set; }
    public decimal? GlobalPercentage { get; set; }
}
