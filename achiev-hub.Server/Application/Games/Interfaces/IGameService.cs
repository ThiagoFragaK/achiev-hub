using achiev_hub.Server.Application.Users;
using achiev_hub.Server.Application.Users.Interfaces;
using achiev_hub.Server.Application.Games;
using achiev_hub.Server.Application.Games.Interfaces;
using achiev_hub.Server.Application.Achievements;
using achiev_hub.Server.Application.Achievements.Interfaces;
using achiev_hub.Server.Application.Goals;
using achiev_hub.Server.Application.Goals.Interfaces;

namespace achiev_hub.Server.Application.Games.Interfaces;
public interface IGameService
{
    Task<IReadOnlyList<GameDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<GameDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<GameDto> CreateAsync(CreateGameRequest request, CancellationToken cancellationToken = default);
    Task<GameDto> UpdateAsync(int id, UpdateGameRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
