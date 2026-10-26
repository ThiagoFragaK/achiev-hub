using achiev_hub.Server.Application.Achievements.Interfaces;
using achiev_hub.Server.Application.Common;
using achiev_hub.Server.Application.Common.Interfaces;
using achiev_hub.Server.Domain.Entities;

namespace achiev_hub.Server.Application.Achievements;

public class AchievementService : IAchievementService
{
    private readonly IRepository<Achievement> _achievements;
    private readonly IRepository<Game> _games;

    public AchievementService(IRepository<Achievement> achievements, IRepository<Game> games)
    {
        _achievements = achievements;
        _games = games;
    }

    public async Task<IReadOnlyList<AchievementRecordDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException(
            "Listing all achievements is disabled. Query by id or use the game achievements endpoint.");
    }

    public async Task<AchievementRecordDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return Map(await GetRequiredAsync(id, cancellationToken));
    }

    public async Task<AchievementRecordDto> CreateAsync(CreateAchievementRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureGameExistsAsync(request.GameId, cancellationToken);
        var apiName = SteamMediaUrls.RequireTrimmedKey(request.ApiName, nameof(request.ApiName));

        var achievement = new Achievement
        {
            GameId = request.GameId,
            ApiName = apiName,
            Name = request.Name.Trim(),
            Description = request.Description,
            ImageUrlLock = SteamMediaUrls.ToStorageHash(request.ImageUrlLock),
            ImageUrlUnlock = SteamMediaUrls.ToStorageHash(request.ImageUrlUnlock),
            GlobalPercentage = request.GlobalPercentage
        };

        await _achievements.AddAsync(achievement, cancellationToken);
        await _achievements.SaveChangesAsync(cancellationToken);
        return Map(achievement);
    }

    public async Task<AchievementRecordDto> UpdateAsync(int id, UpdateAchievementRequest request, CancellationToken cancellationToken = default)
    {
        var achievement = await GetRequiredAsync(id, cancellationToken);
        await EnsureGameExistsAsync(request.GameId, cancellationToken);
        var apiName = SteamMediaUrls.RequireTrimmedKey(request.ApiName, nameof(request.ApiName));

        achievement.GameId = request.GameId;
        achievement.ApiName = apiName;
        achievement.Name = request.Name.Trim();
        achievement.Description = request.Description;
        achievement.ImageUrlLock = SteamMediaUrls.ToStorageHash(request.ImageUrlLock);
        achievement.ImageUrlUnlock = SteamMediaUrls.ToStorageHash(request.ImageUrlUnlock);
        achievement.GlobalPercentage = request.GlobalPercentage;

        _achievements.Update(achievement);
        await _achievements.SaveChangesAsync(cancellationToken);
        return Map(achievement);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var achievement = await GetRequiredAsync(id, cancellationToken);
        _achievements.Remove(achievement);
        await _achievements.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureGameExistsAsync(int gameId, CancellationToken cancellationToken)
    {
        if (!await _games.AnyAsync(game => game.Id == gameId, cancellationToken))
        {
            throw new NotFoundException($"Game {gameId} was not found.");
        }
    }

    private async Task<Achievement> GetRequiredAsync(int id, CancellationToken cancellationToken)
    {
        return await _achievements.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Achievement {id} was not found.");
    }

    private static AchievementRecordDto Map(Achievement achievement) => new()
    {
        Id = achievement.Id,
        GameId = achievement.GameId,
        ApiName = achievement.ApiName,
        Name = achievement.Name,
        Description = achievement.Description,
        ImageUrlLock = achievement.ImageUrlLock,
        ImageUrlUnlock = achievement.ImageUrlUnlock,
        GlobalPercentage = achievement.GlobalPercentage
    };
}
