using achiev_hub.Server.Entities;

namespace achiev_hub.Server.DTOs.Persistence;

public class GoalAchievementDto
{
    public int Id { get; set; }
    public int AchievementId { get; set; }
    public int GoalId { get; set; }
    public GoalAchievementStatus Status { get; set; }
}

public class CreateGoalAchievementRequest
{
    public int AchievementId { get; set; }
    public int GoalId { get; set; }
    public GoalAchievementStatus Status { get; set; } = GoalAchievementStatus.Pending;
}

public class UpdateGoalAchievementRequest
{
    public int AchievementId { get; set; }
    public int GoalId { get; set; }
    public GoalAchievementStatus Status { get; set; }
}
