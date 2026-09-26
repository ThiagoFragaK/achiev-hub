namespace achiev_hub.Server.DTOs;

public class SyncStatusDto
{
    public decimal AvgPercentage { get; set; }
    public decimal AchievementSyncCoverage { get; set; }
    public int OwnedWithStats { get; set; }
    public int SyncedWithStats { get; set; }
    public bool IsUpdating { get; set; }
    public bool IsReady { get; set; }
    public int Status { get; set; }
    public string? StatusLabel { get; set; }
    public IReadOnlyList<SyncJobStatusDto> Jobs { get; set; } = [];
}

public class SyncJobStatusDto
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int ProgressDone { get; set; }
    public int ProgressTotal { get; set; }
    public string? LastError { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public class EnqueueSyncResponseDto
{
    public bool Success { get; set; } = true;
    public string Message { get; set; } = "Sync enqueued.";
    public IReadOnlyList<int> JobIds { get; set; } = [];
}
