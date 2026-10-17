namespace achiev_hub.Server.Application.Games;

public class LibraryGameFilterDto
{
    public string? Name { get; set; }
    public double? MinHours { get; set; }
    public bool? HasAchievements { get; set; }
    /// <summary>playtime (default) | name | achievements</summary>
    public string? Sort { get; set; }
}
