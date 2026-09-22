namespace achiev_hub.Server.DTOs;

public class UserStatsDto
{
    public IReadOnlyList<DailyAchievementCountDto> AchievementsLast14Days { get; set; } = [];
    public IReadOnlyList<YearlyAchievementCountDto> AchievementsPerYear { get; set; } = [];
    public decimal AveragePercentage { get; set; }
}

public class DailyAchievementCountDto
{
    /// <summary>Local calendar day, formatted as yyyy-MM-dd.</summary>
    public string Date { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class YearlyAchievementCountDto
{
    public int Year { get; set; }
    public int Count { get; set; }
}
