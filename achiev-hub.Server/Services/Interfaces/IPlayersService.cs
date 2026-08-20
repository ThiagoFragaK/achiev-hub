using achiev_hub.Server.DTOs;

namespace achiev_hub.Server.Services.Interfaces;

public interface IPlayersService
{
    Task<PlayerDto?> GetPlayerAsync(string steamId, CancellationToken cancellationToken = default);
}
