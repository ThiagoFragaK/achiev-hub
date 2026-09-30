namespace achiev_hub.Server.DTOs.Auth;

public class LoginRequest
{
    public string SteamId { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class GuestRequest
{
    public string SteamId { get; set; } = string.Empty;
}

public class AuthUserDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? SteamId { get; set; }
    public string Role { get; set; } = string.Empty;
    public bool SteamLibraryPublic { get; set; } = true;
}

public class LoginResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string TokenType { get; set; } = "Bearer";
    public AuthUserDto User { get; set; } = null!;
    public bool SyncEnqueued { get; set; }
    public string? SyncMessage { get; set; }
    public SteamSync.Shared.SyncSummaryDto Sync { get; set; } = SteamSync.Shared.SyncSummaryDto.Empty;
}

public class RegisterResponseDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? SteamId { get; set; }
    public int Status { get; set; }
    public string StatusLabel { get; set; } = string.Empty;
    public bool SteamLibraryPublic { get; set; } = true;
    public IReadOnlyList<Guid> JobIds { get; set; } = [];
    public string Message { get; set; } = "Registration successful. Preparing your library…";
    public SteamSync.Shared.SyncSummaryDto Sync { get; set; } = SteamSync.Shared.SyncSummaryDto.Empty;
}
