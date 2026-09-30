using SteamSync.Shared;

namespace achiev_hub.Server.Entities;

/// <summary>Tracks per-user sync progress written by steam-sync worker.</summary>
public class UserSyncStatus
{
    public int UserId { get; set; }
    public DateTime? LastPartialSync { get; set; }
    public DateTime? LastFullSync { get; set; }
    public decimal SyncProgressPercent { get; set; }
    public int GamesSyncedCount { get; set; }
    public int TotalGamesCount { get; set; }
    public SyncStatus Status { get; set; } = SyncStatus.Pending;
    public string? LastError { get; set; }
    public Guid? LastJobId { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>In-flight claim; skip enqueue while UtcNow &lt; LockedUntil.</summary>
    public DateTimeOffset? LockedUntil { get; set; }

    /// <summary>Last auto-triggered enqueue (login / lazy refresh); max 1 per day.</summary>
    public DateTimeOffset? LastAutoEnqueueAt { get; set; }

    /// <summary>UTC date of the manual enqueue counter window.</summary>
    public DateOnly? ManualEnqueueDate { get; set; }

    /// <summary>Manual sync publishes on <see cref="ManualEnqueueDate"/> (max 2).</summary>
    public int ManualEnqueueCount { get; set; }

    public User User { get; set; } = null!;
}
