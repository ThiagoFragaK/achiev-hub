namespace achiev_hub.Server.DTOs.Persistence;

public class UserDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? SteamId { get; set; }
}

public class CreateUserRequest
{
    public string Email { get; set; } = string.Empty;
    public string? SteamId { get; set; }
    public string Password { get; set; } = string.Empty;
}

public class UpdateUserRequest
{
    public string Email { get; set; } = string.Empty;
    public string? SteamId { get; set; }
    public string? Password { get; set; }
}
