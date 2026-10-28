namespace achiev_hub.Server.Application.Games;

public class RecentAchievementDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public int AppId { get; set; }
    public string? GameName { get; set; }
    public string Unlocked { get; set; } = "-";
    public decimal? GlobalPercentage { get; set; }
}
