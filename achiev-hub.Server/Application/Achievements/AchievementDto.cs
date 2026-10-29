namespace achiev_hub.Server.Application.Achievements;

public class AchievementDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public string Unlocked { get; set; } = "-";
    public decimal? GlobalPercentage { get; set; }
}
