using achiev_hub.Server.Domain.Interfaces;

namespace achiev_hub.Server.Domain.Entities;

public class Goal : IEntity
{
    public int Id { get; set; }
    public int GameId { get; set; }
    public int UserId { get; set; }
    public decimal PercentageGoal { get; set; }
    public int AchievementsMissing { get; set; }

    public Game Game { get; set; } = null!;
    public User User { get; set; } = null!;
    public ICollection<GoalAchievement> GoalAchievements { get; set; } = [];
}
