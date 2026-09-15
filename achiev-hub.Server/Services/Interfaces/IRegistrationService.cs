using achiev_hub.Server.DTOs.Auth;
using achiev_hub.Server.DTOs.Persistence;

namespace achiev_hub.Server.Services.Interfaces;

public interface IRegistrationService
{
    Task<object> SendVerificationAsync(string email, CancellationToken cancellationToken = default);
    Task<object> ConfirmCodeAsync(string email, string code, CancellationToken cancellationToken = default);
    Task<object> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
}
