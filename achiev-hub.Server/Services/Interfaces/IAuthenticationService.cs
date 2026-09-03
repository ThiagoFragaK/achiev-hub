using achiev_hub.Server.DTOs.Auth;

namespace achiev_hub.Server.Services.Interfaces;

public interface IAuthenticationService
{
    Task<object> LoginAsync(string steamId, string password, CancellationToken cancellationToken = default);
    Task LogoutAsync(int userId, CancellationToken cancellationToken = default);
}
