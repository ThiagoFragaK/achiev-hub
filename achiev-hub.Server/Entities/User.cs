using System.Text.Json.Serialization;

namespace achiev_hub.Server.Entities;

public class User : IEntity
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? SteamId { get; set; }

    [JsonIgnore]
    public string Password { get; set; } = string.Empty;

    public string Role { get; set; } = "user";
    public int Status { get; set; } = 1;
    public bool IsEmailVerified { get; set; }

    [JsonIgnore]
    public int TokenVersion { get; set; }

    public DateTime? LastLogin { get; set; }
    public int Playtime2WeeksMinutes { get; set; }
    public decimal AvgPercentage { get; set; }

    public ICollection<UsersGame> UsersGames { get; set; } = [];
    public ICollection<UsersAchievement> UsersAchievements { get; set; } = [];
    public ICollection<Goal> Goals { get; set; } = [];
}
