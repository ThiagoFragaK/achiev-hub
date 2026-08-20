namespace achiev_hub.Server.Models;

public class GameSchema
{
    public string? GameName { get; set; }
    public List<GameSchemaAchievement> Achievements { get; set; } = [];
}
