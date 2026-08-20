namespace achiev_hub.Server.DTOs.Persistence;

public class GameDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? GameSteamId { get; set; }
    public int PlayTime { get; set; }
}

public class CreateGameRequest
{
    public string Name { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? GameSteamId { get; set; }
    public int PlayTime { get; set; }
}

public class UpdateGameRequest
{
    public string Name { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? GameSteamId { get; set; }
    public int PlayTime { get; set; }
}
