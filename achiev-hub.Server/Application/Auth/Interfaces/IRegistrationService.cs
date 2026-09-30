using achiev_hub.Server.Application.Auth;

namespace achiev_hub.Server.Application.Auth.Interfaces;

public interface IRegistrationService
{
    Task<object> ValidateSteamAsync(string steamId, CancellationToken cancellationToken = default);
    Task<object> SendVerificationAsync(string email, string steamId, CancellationToken cancellationToken = default);
    Task<object> ConfirmCodeAsync(string email, string code, CancellationToken cancellationToken = default);
    Task<object> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
}
