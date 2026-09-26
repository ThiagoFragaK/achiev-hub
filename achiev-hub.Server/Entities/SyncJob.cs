using achiev_hub.Server.Enums;

namespace achiev_hub.Server.Entities;

public class SyncJob : IEntity
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public string? SteamId { get; set; }
    public SyncJobType Type { get; set; }
    public int? AppId { get; set; }
    public SyncJobStatus Status { get; set; } = SyncJobStatus.Pending;
    public int Cursor { get; set; }
    public int ProgressDone { get; set; }
    public int ProgressTotal { get; set; }
    public int Attempts { get; set; }
    public string? LastError { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset AvailableAt { get; set; } = DateTimeOffset.UtcNow;

    public User? User { get; set; }
}
