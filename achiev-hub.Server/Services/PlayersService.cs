using achiev_hub.Server.DTOs;
using achiev_hub.Server.Repositories.Interfaces;
using achiev_hub.Server.Services.Interfaces;

namespace achiev_hub.Server.Services;

public class PlayersService : IPlayersService
{
    private readonly ISteamRepository _steamRepository;

    public PlayersService(ISteamRepository steamRepository)
    {
        _steamRepository = steamRepository;
    }

    public async Task<PlayerDto?> GetPlayerAsync(string steamId, CancellationToken cancellationToken = default)
    {
        var player = await _steamRepository.GetPlayerBySteamIdAsync(steamId, cancellationToken);
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
