namespace achiev_hub.Server.Application.Auth;

public class SendVerificationRequest
{
    public string Email { get; set; } = string.Empty;
    public string SteamId { get; set; } = string.Empty;
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

public class ValidateSteamRequest
{
    public string SteamId { get; set; } = string.Empty;
}

public class RegisterRequest
{
    public string SteamId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string EmailVerifiedToken { get; set; } = string.Empty;
}

public class RegisterSyncSummaryDto
{
    public string Status { get; set; } = string.Empty;
    public bool Enqueued { get; set; }
    public string? LastError { get; set; }
}

public class RegisterResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public AuthUserDto User { get; set; } = null!;
    public int Status { get; set; }
    public string StatusLabel { get; set; } = string.Empty;
    public RegisterSyncSummaryDto Sync { get; set; } = null!;
}
