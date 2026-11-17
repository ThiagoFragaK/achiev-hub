using achiev_hub.Server.Application.Common;
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

    Task<PagedResultDto<AchievementPageRow>> GetPagedForUserAsync(
        int userId,
        int gameId,
        AchievementPageQuery query,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Achievement>> GetTrackedByGameIdAsync(
        int gameId,
        CancellationToken cancellationToken = default);

    void Add(Achievement achievement);
}

public sealed class AchievementPageQuery
{
    public string? Name { get; set; }
    public string? Status { get; set; }
    public string? Sort { get; set; }
    public string? Global { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}

public sealed class AchievementPageRow
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrlLock { get; set; }
    public string? ImageUrlUnlock { get; set; }
    public decimal? GlobalPercentage { get; set; }
    public bool IsUnlocked { get; set; }
    public DateTime? UnlockUtc { get; set; }
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
