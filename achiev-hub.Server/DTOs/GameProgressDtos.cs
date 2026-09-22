namespace achiev_hub.Server.DTOs;

public class GameProgressDto
{
    public IReadOnlyList<GameProgressPointDto> Points { get; set; } = [];

    /// <summary>Spacing between points: day, week, month or year.</summary>
    public string Granularity { get; set; } = GameProgressGranularity.Day;

    public int UnlockedCount { get; set; }
    public int TotalCount { get; set; }

    /// <summary>True when nothing is left to unlock, which ends the timeline on the last unlock.</summary>
    public bool IsCompleted { get; set; }
}

public class GameProgressPointDto
{
    /// <summary>Local calendar day, formatted as yyyy-MM-dd.</summary>
    public string Date { get; set; } = string.Empty;

    /// <summary>Achievements unlocked up to and including this day.</summary>
    public int Count { get; set; }

    public decimal Percentage { get; set; }
}

public static class GameProgressGranularity
{
    public const string Day = "day";
    public const string Week = "week";
    public const string Month = "month";
    public const string Year = "year";
}
