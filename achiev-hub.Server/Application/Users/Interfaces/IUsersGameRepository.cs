namespace achiev_hub.Server.Application.Users.Interfaces;

public interface IUsersGameRepository
{
    Task<IReadOnlyList<UsersGameAchievementStatsRow>> GetGamesWithAchievementStatsAsync(
        int userId,
        bool excludeCompleted = false,
        string? nameQuery = null,
        IReadOnlyCollection<int>? excludeGameIds = null,
        int? take = null,
        CancellationToken cancellationToken = default);

    Task<(decimal Average, int GamesInAverage)> GetAveragePercentageStatsAsync(
        int userId,
        CancellationToken cancellationToken = default);
}

public sealed class UsersGameAchievementStatsRow
{
    public int GameId { get; set; }
    public string GameSteamId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public decimal AchievementsPercentage { get; set; }
}
