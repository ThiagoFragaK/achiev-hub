namespace achiev_hub.Server.Entities;

public class Game : IEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? GameSteamId { get; set; }
    public string? Developers { get; set; }
    public string? Publishers { get; set; }
    public bool? HasCommunityVisibleStats { get; set; }

    public ICollection<Achievement> Achievements { get; set; } = [];
    public ICollection<UsersGame> UsersGames { get; set; } = [];
    public ICollection<Goal> Goals { get; set; } = [];
    public ICollection<UsersAchievement> UsersAchievements { get; set; } = [];
}
