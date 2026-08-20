namespace achiev_hub.Server.DTOs;

public class AchievementDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public string Unlocked { get; set; } = "-";
}
