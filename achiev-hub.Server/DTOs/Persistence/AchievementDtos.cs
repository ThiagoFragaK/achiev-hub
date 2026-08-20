namespace achiev_hub.Server.DTOs.Persistence;

public class AchievementRecordDto
{
    public int Id { get; set; }
    public int GameId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrlLock { get; set; }
    public string? ImageUrlUnlock { get; set; }
    public decimal? GlobalPercentage { get; set; }
}

public class CreateAchievementRequest
{
    public int GameId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrlLock { get; set; }
    public string? ImageUrlUnlock { get; set; }
    public decimal? GlobalPercentage { get; set; }
}

public class UpdateAchievementRequest
{
    public int GameId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrlLock { get; set; }
    public string? ImageUrlUnlock { get; set; }
    public decimal? GlobalPercentage { get; set; }
}
