namespace achiev_hub.Server.Entities;

public class User : IEntity
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? SteamId { get; set; }
    public string Password { get; set; } = string.Empty;

    public ICollection<UsersGame> UsersGames { get; set; } = [];
    public ICollection<UsersAchievement> UsersAchievements { get; set; } = [];
    public ICollection<Goal> Goals { get; set; } = [];
}
