namespace achiev_hub.Server.Application.Achievements;

public class AchievementSummaryDto
{
    public int Unlocked { get; set; }
    public int Locked { get; set; }
    public int Total { get; set; }
    public double Percentage { get; set; }
}
