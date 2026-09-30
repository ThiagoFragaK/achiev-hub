using achiev_hub.Server.Domain.Entities;
using achiev_hub.Server.Domain.Interfaces;

namespace achiev_hub.Server.Application.Goals;

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
