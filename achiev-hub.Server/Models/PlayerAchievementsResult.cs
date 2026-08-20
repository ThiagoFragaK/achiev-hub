namespace achiev_hub.Server.Models;

public class PlayerAchievementsResult
{
    public bool Success { get; set; }
    public string? GameName { get; set; }
    public List<PlayerAchievement> Achievements { get; set; } = [];
}
