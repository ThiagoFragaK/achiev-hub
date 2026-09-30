using achiev_hub.Server.Application.Achievements;
using achiev_hub.Server.Application.Achievements.Interfaces;

namespace achiev_hub.Server.Application.Games;

public class RecentGameDto
{
    public int AppId { get; set; }
    public string? Name { get; set; }
    public double PlayTimeWeeks { get; set; }
    public double PlayTimeTotal { get; set; }
    public string? Image { get; set; }
    public AchievementSummaryDto Achievements { get; set; } = new();
}
