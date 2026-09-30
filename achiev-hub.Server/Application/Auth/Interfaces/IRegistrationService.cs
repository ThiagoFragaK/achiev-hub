using achiev_hub.Server.Application.Auth;
using achiev_hub.Server.Application.Auth.Interfaces;
using achiev_hub.Server.Application.Users;
using achiev_hub.Server.Application.Users.Interfaces;
using achiev_hub.Server.Application.Games;
using achiev_hub.Server.Application.Games.Interfaces;
using achiev_hub.Server.Application.Achievements;
using achiev_hub.Server.Application.Achievements.Interfaces;
using achiev_hub.Server.Application.Goals;
using achiev_hub.Server.Application.Goals.Interfaces;

namespace achiev_hub.Server.Application.Auth.Interfaces;
public interface IRegistrationService
{
    Task<object> SendVerificationAsync(string email, CancellationToken cancellationToken = default);
    Task<object> ConfirmCodeAsync(string email, string code, CancellationToken cancellationToken = default);
    Task<object> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
}
