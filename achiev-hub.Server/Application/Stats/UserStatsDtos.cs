namespace achiev_hub.Server.Application.Stats;

public class UserStatsDto
{
    public IReadOnlyList<DailyAchievementCountDto> AchievementsLast14Days { get; set; } = [];
    public IReadOnlyList<YearlyAchievementCountDto> AchievementsPerYear { get; set; } = [];
    public decimal AveragePercentage { get; set; }
    public int TotalAchievements { get; set; }
    public int GamesWithAchievements { get; set; }
}

public class DailyAchievementCountDto
{
    public string Date { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class YearlyAchievementCountDto
{
    public int Year { get; set; }
    public int Count { get; set; }
}

public class AchievementsByMonthDto
{
    public IReadOnlyList<int> AvailableYears { get; set; } = [];
    public IReadOnlyList<YearMonthSeriesDto> Series { get; set; } = [];
}

public class YearMonthSeriesDto
{
    public int Year { get; set; }
    public IReadOnlyList<int> Counts { get; set; } = [];
}

public class GamesPercentageBandsDto
{
    public IReadOnlyList<PercentageBandCountDto> Bands { get; set; } = [];
}

public class PercentageBandCountDto
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class GamesForAverageDto
{
    public IReadOnlyList<GameForAverageDto> Games { get; set; } = [];
}

public class GameForAverageDto
{
    public int AppId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal AchievementsPercentage { get; set; }
}
