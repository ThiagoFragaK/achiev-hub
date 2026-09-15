namespace achiev_hub.Server.Entities;

public class EmailVerification : IEntity
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string CodeHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime LastSentAt { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public string? ConsumeTokenHash { get; set; }
    public DateTime? ConsumeTokenExpiresAt { get; set; }
}
