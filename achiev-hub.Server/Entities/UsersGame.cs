namespace achiev_hub.Server.Entities;

public class UsersGame : IEntity
{
    public int Id { get; set; }
    public int GameId { get; set; }
    public int UserId { get; set; }
    public decimal AchievementsPercentage { get; set; }

    public Game Game { get; set; } = null!;
    public User User { get; set; } = null!;
}
