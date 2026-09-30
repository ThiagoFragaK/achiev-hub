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
using achiev_hub.Server.Application.Common;
using achiev_hub.Server.Application.Common.Interfaces;

namespace achiev_hub.Server.Application.Steam;

public class PlayersService : IPlayersService
{
    private readonly ISteamApiClient _steamApiClient;

    public PlayersService(ISteamApiClient steamApiClient)
    {
        _steamApiClient = steamApiClient;
    }

    public async Task<PlayerDto?> GetPlayerAsync(string steamId, CancellationToken cancellationToken = default)
    {
        var player = await _steamApiClient.GetPlayerBySteamIdAsync(steamId, cancellationToken);
        if (player is null)
        {
            return null;
        }

        return new PlayerDto
        {
            SteamId = player.SteamId,
            PersonaName = player.PersonaName,
            ProfileUrl = player.ProfileUrl,
            Avatar = player.Avatar,
            AvatarFull = player.AvatarFull,
            CommunityVisibilityState = player.CommunityVisibilityState,
            PersonaState = player.PersonaState
        };
    }
}
