using achiev_hub.Server.Application.Users;
using achiev_hub.Server.Application.Users.Interfaces;
using achiev_hub.Server.Application.Games;
using achiev_hub.Server.Application.Games.Interfaces;
using achiev_hub.Server.Application.Achievements;
using achiev_hub.Server.Application.Achievements.Interfaces;
using achiev_hub.Server.Application.Goals;
using achiev_hub.Server.Application.Goals.Interfaces;

namespace achiev_hub.Server.Application.Users.Interfaces;
public interface IUsersGameService
{
    Task<IReadOnlyList<UsersGameDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<UsersGameDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<UsersGameDto> CreateAsync(CreateUsersGameRequest request, CancellationToken cancellationToken = default);
    Task<UsersGameDto> UpdateAsync(int id, UpdateUsersGameRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
