using achiev_hub.Server.DTOs.Persistence;

namespace achiev_hub.Server.Services.Interfaces;

public interface IUsersGameService
{
    Task<IReadOnlyList<UsersGameDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<UsersGameDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<UsersGameDto> CreateAsync(CreateUsersGameRequest request, CancellationToken cancellationToken = default);
    Task<UsersGameDto> UpdateAsync(int id, UpdateUsersGameRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
