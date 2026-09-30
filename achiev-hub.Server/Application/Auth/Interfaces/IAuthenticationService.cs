using achiev_hub.Server.Application.Auth;
using achiev_hub.Server.Application.Auth.Interfaces;

namespace achiev_hub.Server.Application.Auth.Interfaces;
public interface IAuthenticationService
{
    Task<object> LoginAsync(string steamId, string password, CancellationToken cancellationToken = default);
    object ContinueAsGuest(string steamId);
    Task LogoutAsync(int userId, CancellationToken cancellationToken = default);
}
