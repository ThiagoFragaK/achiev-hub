using achiev_hub.Server.Domain.Entities;
using achiev_hub.Server.Domain.Enums;

namespace achiev_hub.Server.Application.Goals;

public class GoalDto
{
    public int Id { get; set; }
    public int AppId { get; set; }
    public string GameName { get; set; } = string.Empty;
    public string? GameIcon { get; set; }
    public decimal PercentageGoal { get; set; }
    public int AchievementsMissing { get; set; }
    public int AchievementsTotal { get; set; }
    public GoalStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public IReadOnlyList<int> AchievementIds { get; set; } = [];
}

public class CreateGoalRequest
{
    public int AppId { get; set; }
    public decimal PercentageGoal { get; set; }
    public IReadOnlyList<int> AchievementIds { get; set; } = [];
}

public class UpdateGoalRequest
{
    public decimal PercentageGoal { get; set; }
}

public class AttachGoalAchievementsRequest
{
    public IReadOnlyList<int> AchievementIds { get; set; } = [];
}

public class ReplaceGoalAchievementsRequest
{
    public IReadOnlyList<int> AchievementIds { get; set; } = [];
    public decimal PercentageGoal { get; set; }
}

public class GoalAchievementDetailDto
{
    public int AchievementId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public decimal? GlobalPercentage { get; set; }
    public GoalAchievementStatus Status { get; set; }
    public bool Unlocked { get; set; }
}

public class EligibleGameDto
{
    public int AppId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal AchievementsPercentage { get; set; }
    public string? Icon { get; set; }
}

public class GoalsCompletedSeriesDto
{
    public IReadOnlyList<string> Labels { get; set; } = [];
    public IReadOnlyList<int> Counts { get; set; } = [];
}
