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
}

public class LoginResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string TokenType { get; set; } = "Bearer";
    public AuthUserDto User { get; set; } = null!;
}
