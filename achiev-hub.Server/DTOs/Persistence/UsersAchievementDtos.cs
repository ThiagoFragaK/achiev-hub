namespace achiev_hub.Server.DTOs.Persistence;

public class UsersAchievementDto
{
    public int Id { get; set; }
    public int AchievementId { get; set; }
    public int UserId { get; set; }
    public int GameId { get; set; }
    public DateTime? UnlockDate { get; set; }
}

public class CreateUsersAchievementRequest
{
    public int AchievementId { get; set; }
    public int UserId { get; set; }
    public int GameId { get; set; }
    public DateTime? UnlockDate { get; set; }
}

public class UpdateUsersAchievementRequest
{
    public int AchievementId { get; set; }
    public int UserId { get; set; }
    public int GameId { get; set; }
    public DateTime? UnlockDate { get; set; }
}
