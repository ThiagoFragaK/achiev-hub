using achiev_hub.Server.Infrastructure.Steam.Contracts;

namespace achiev_hub.Server.Application.Steam;

public class SyncStatusDto
{
    public int Status { get; set; }
    public string? StatusLabel { get; set; }
    public bool IsReady { get; set; }
    public bool IsUpdating { get; set; }
    public SyncSummaryDto Sync { get; set; } = SyncSummaryDto.Empty;
}
