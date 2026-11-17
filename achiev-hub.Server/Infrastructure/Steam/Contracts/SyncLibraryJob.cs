namespace achiev_hub.Server.Infrastructure.Steam.Contracts;

/// <summary>Full owned-library upsert, then fan-out remaining achievement syncs.</summary>
public class SyncLibraryJob
{
    public Guid JobId { get; set; } = Guid.NewGuid();

    public int UserId { get; set; }

    public string SteamId { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int RetryCount { get; set; }
}
