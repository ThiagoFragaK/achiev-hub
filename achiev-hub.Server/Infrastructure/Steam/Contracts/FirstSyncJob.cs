namespace achiev_hub.Server.Infrastructure.Steam.Contracts;

/// <summary>High-priority first sync after registration (recently played games).</summary>
public class FirstSyncJob
{
    public Guid JobId { get; set; } = Guid.NewGuid();

    public int UserId { get; set; }

    public string SteamId { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int RetryCount { get; set; }
}
