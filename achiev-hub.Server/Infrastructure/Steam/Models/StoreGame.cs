namespace achiev_hub.Server.Infrastructure.Steam.Models;

public class StoreGame
{
    public string? Name { get; set; }
    public string? HeaderImage { get; set; }
    public List<string> Developers { get; set; } = [];
    public List<string> Publishers { get; set; } = [];
}
