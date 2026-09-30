namespace achiev_hub.Server.DTOs;

public class LibraryGameDto
{
    public int AppId { get; set; }
    public string? Name { get; set; }
    public string? Icon { get; set; }
    public double Playtime { get; set; }
    public string? NotPlayedSince { get; set; }
    public bool HasAchievements { get; set; }
    /// <summary>True when achievements have not been imported yet.</summary>
    public bool ImportPending { get; set; }
}
