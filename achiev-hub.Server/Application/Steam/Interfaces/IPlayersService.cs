using achiev_hub.Server.Application.Games;
using achiev_hub.Server.Application.Games.Interfaces;
using achiev_hub.Server.Application.Achievements;
using achiev_hub.Server.Application.Achievements.Interfaces;
using achiev_hub.Server.Application.Steam;
using achiev_hub.Server.Application.Steam.Interfaces;
using achiev_hub.Server.Application.Stats;
using achiev_hub.Server.Application.Stats.Interfaces;
using achiev_hub.Server.Application.Users;
using achiev_hub.Server.Application.Users.Interfaces;
using achiev_hub.Server.Application.Goals;
using achiev_hub.Server.Application.Goals.Interfaces;
using achiev_hub.Server.Application.Auth;
using achiev_hub.Server.Application.Auth.Interfaces;

namespace achiev_hub.Server.Application.Steam.Interfaces;
public interface IPlayersService
{
    Task<PlayerDto?> GetPlayerAsync(string steamId, CancellationToken cancellationToken = default);
}
