using achiev_hub.Server.DTOs.Persistence;

namespace achiev_hub.Server.Services.Interfaces;

public interface IGameService
{
    Task<IReadOnlyList<GameDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<GameDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<GameDto> CreateAsync(CreateGameRequest request, CancellationToken cancellationToken = default);
    Task<GameDto> UpdateAsync(int id, UpdateGameRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
