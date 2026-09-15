namespace achiev_hub.Server.DTOs.Auth;

public class SendVerificationRequest
{
    public string Email { get; set; } = string.Empty;
}

public class ConfirmCodeRequest
{
    public string Email { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}

public class ConfirmCodeResponseDto
{
    public string EmailVerifiedToken { get; set; } = string.Empty;
}

public class RegisterRequest
{
    public string SteamId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string EmailVerifiedToken { get; set; } = string.Empty;
}
