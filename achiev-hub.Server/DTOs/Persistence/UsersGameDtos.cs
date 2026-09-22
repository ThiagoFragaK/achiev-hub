namespace achiev_hub.Server.DTOs.Persistence;

public class UsersGameDto
{
    public int Id { get; set; }
    public int GameId { get; set; }
    public int UserId { get; set; }
    public decimal AchievementsPercentage { get; set; }
    public int PlaytimeMinutes { get; set; }
    public long? LastPlayedUnix { get; set; }
}

public class CreateUsersGameRequest
{
    public int GameId { get; set; }
    public int UserId { get; set; }
    public decimal AchievementsPercentage { get; set; }
    public int PlaytimeMinutes { get; set; }
    public long? LastPlayedUnix { get; set; }
}

public class UpdateUsersGameRequest
{
    public int GameId { get; set; }
    public int UserId { get; set; }
    public decimal AchievementsPercentage { get; set; }
    public int PlaytimeMinutes { get; set; }
    public long? LastPlayedUnix { get; set; }
}
