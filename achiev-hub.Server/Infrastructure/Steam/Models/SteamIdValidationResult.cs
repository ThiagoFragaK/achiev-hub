namespace achiev_hub.Server.Infrastructure.Steam.Models;

public enum SteamIdValidationStatus
{
    InvalidFormat,
    NotFound,
    Valid
}

public sealed class SteamIdValidationResult
{
    public SteamIdValidationStatus Status { get; init; }
    public string? SteamId { get; init; }
    public string? PersonaName { get; init; }
    public string? Avatar { get; init; }
    public bool IsLibraryPublic { get; init; }
    public int? CommunityVisibilityState { get; init; }

    public static SteamIdValidationResult InvalidFormat() =>
        new() { Status = SteamIdValidationStatus.InvalidFormat };

    public static SteamIdValidationResult NotFound() =>
        new() { Status = SteamIdValidationStatus.NotFound };

    public static SteamIdValidationResult Valid(Player player)
    {
        var isLibraryPublic = player.CommunityVisibilityState == 3;
        return new SteamIdValidationResult
        {
            Status = SteamIdValidationStatus.Valid,
            SteamId = player.SteamId,
            PersonaName = player.PersonaName,
            Avatar = player.AvatarFull ?? player.Avatar,
            IsLibraryPublic = isLibraryPublic,
            CommunityVisibilityState = player.CommunityVisibilityState
        };
    }
}
