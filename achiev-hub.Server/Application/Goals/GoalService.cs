using achiev_hub.Server.Application.Achievements.Interfaces;
using achiev_hub.Server.Application.Common;
using achiev_hub.Server.Application.Common.Interfaces;
using achiev_hub.Server.Application.Games;
using achiev_hub.Server.Application.Games.Interfaces;
using achiev_hub.Server.Application.Goals.Interfaces;
using achiev_hub.Server.Application.Users.Interfaces;
using achiev_hub.Server.Domain.Entities;
using achiev_hub.Server.Domain.Enums;

namespace achiev_hub.Server.Application.Goals;

public class GoalService : IGoalService
{
    private readonly IGoalRepository _goals;
    private readonly IUsersGameRepository _usersGames;
    private readonly IUsersAchievementRepository _usersAchievements;
    private readonly IGameRepository _games;
    private readonly IAchievementRepository _achievements;
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _unitOfWork;

    public GoalService(
        IGoalRepository goals,
        IUsersGameRepository usersGames,
        IUsersAchievementRepository usersAchievements,
        IGameRepository games,
        IAchievementRepository achievements,
        IUserRepository users,
        IUnitOfWork unitOfWork)
    {
        _goals = goals;
        _usersGames = usersGames;
        _usersAchievements = usersAchievements;
        _games = games;
        _achievements = achievements;
        _users = users;
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResultDto<GoalDto>> GetForUserAsync(
        int userId,
        int page = 1,
        int pageSize = 15,
        GoalListFilterDto? filters = null,
        CancellationToken cancellationToken = default)
    {
        string? gameSteamId = null;
        if (filters?.AppId is int appId && appId > 0)
        {
            gameSteamId = appId.ToString();
        }

        var pageResult = await _goals.GetPageForUserAsync(
            userId,
            filters?.Status,
            gameSteamId,
            filters?.MinEndGoal,
            filters?.MinCompletion,
            page,
            pageSize,
            cancellationToken);

        return PagedResultDto<GoalDto>.From(
            pageResult.Data.Select(Map).ToList(),
            pageResult.Pagination);
    }

    public async Task<IReadOnlyList<GoalFilterGameDto>> GetFilterGamesAsync(
        int userId,
        GoalStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var rows = await _goals.GetFilterGamesAsync(userId, status, cancellationToken);

        var games = new List<GoalFilterGameDto>();
        foreach (var row in rows)
        {
            if (!int.TryParse(row.GameSteamId, out var appId))
            {
                continue;
            }

            games.Add(new GoalFilterGameDto
            {
                AppId = appId,
                GameName = row.Name
            });
        }

        return games;
    }

    public async Task<GoalDto> GetByIdAsync(int userId, int id, CancellationToken cancellationToken = default)
    {
        return Map(await GetOwnedGoalAsync(userId, id, track: false, cancellationToken));
    }

    public async Task<GoalDto?> GetActiveByAppIdAsync(int userId, int appId, CancellationToken cancellationToken = default)
    {
        var goal = await _goals.GetActiveBySteamAppIdAsync(userId, appId.ToString(), cancellationToken);
        return goal is null ? null : Map(goal);
    }

    public async Task<GoalDto> CreateAsync(int userId, CreateGoalRequest request, CancellationToken cancellationToken = default)
    {
        if (request.AppId <= 0)
        {
            throw new BadRequestException("AppId is required.");
        }

        var steamAppId = request.AppId.ToString();
        var gameId = await _games.GetIdBySteamIdAsync(steamAppId, cancellationToken)
            ?? throw new NotFoundException($"Game {request.AppId} was not found.");

        var percentage = await _usersGames.GetAchievementPercentageAsync(userId, gameId, cancellationToken)
            ?? throw new NotFoundException($"Game {request.AppId} is not in your library.");

        if (percentage >= 100m)
        {
            throw new BadRequestException("Cannot create a goal for a fully completed game.");
        }

        if (request.PercentageGoal <= percentage || request.PercentageGoal > 100m)
        {
            throw new BadRequestException(
                $"Percentage goal must be greater than current progress ({percentage}%) and at most 100.");
        }

        if (await _goals.HasActiveAsync(userId, gameId, cancellationToken: cancellationToken))
        {
            throw new ConflictException("This game already has an active goal.");
        }

        var achievementIds = (request.AchievementIds ?? [])
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        await ValidateLockedAchievementsAsync(userId, gameId, achievementIds, cancellationToken);

        var goal = new Goal
        {
            UserId = userId,
            GameId = gameId,
            PercentageGoal = request.PercentageGoal,
            AchievementsMissing = achievementIds.Count,
            Status = GoalStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };

        foreach (var achievementId in achievementIds)
        {
            goal.GoalAchievements.Add(new GoalAchievement
            {
                AchievementId = achievementId,
                Status = GoalAchievementStatus.Pending
            });
        }

        _goals.Add(goal);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await RefreshGoalEntityAsync(goal, percentage, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(userId, goal.Id, cancellationToken);
    }

    public async Task<GoalDto> UpdateAsync(int userId, int id, UpdateGoalRequest request, CancellationToken cancellationToken = default)
    {
        var goal = await GetOwnedGoalAsync(userId, id, track: true, cancellationToken);
        if (goal.Status != GoalStatus.Active)
        {
            throw new ConflictException("Only active goals can be updated.");
        }

        var currentPercent = await _usersGames.GetAchievementPercentageAsync(userId, goal.GameId, cancellationToken) ?? 0m;
        if (request.PercentageGoal <= currentPercent || request.PercentageGoal > 100m)
        {
            throw new BadRequestException(
                $"Percentage goal must be greater than current progress ({currentPercent}%) and at most 100.");
        }

        goal.PercentageGoal = request.PercentageGoal;
        await RefreshGoalEntityAsync(goal, currentPercent, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(userId, goal.Id, cancellationToken);
    }

    public async Task<GoalDto> AttachAchievementsAsync(
        int userId,
        int id,
        AttachGoalAchievementsRequest request,
        CancellationToken cancellationToken = default)
    {
        var goal = await GetOwnedGoalAsync(userId, id, track: true, cancellationToken);
        if (goal.Status != GoalStatus.Active)
        {
            throw new ConflictException("Achievements can only be attached to an active goal.");
        }

        var achievementIds = (request.AchievementIds ?? [])
            .Where(aid => aid > 0)
            .Distinct()
            .ToList();

        if (achievementIds.Count == 0)
        {
            throw new BadRequestException("At least one achievement id is required.");
        }

        var existingIds = goal.GoalAchievements.Select(ga => ga.AchievementId).ToHashSet();
        var toAdd = achievementIds.Where(aid => !existingIds.Contains(aid)).ToList();
        if (toAdd.Count == 0)
        {
            return await GetByIdAsync(userId, id, cancellationToken);
        }

        await ValidateLockedAchievementsAsync(userId, goal.GameId, toAdd, cancellationToken);

        foreach (var achievementId in toAdd)
        {
            goal.GoalAchievements.Add(new GoalAchievement
            {
                GoalId = goal.Id,
                AchievementId = achievementId,
                Status = GoalAchievementStatus.Pending
            });
        }

        var (unlocked, total) = await GetUserAchievementCountsAsync(userId, goal.GameId, cancellationToken);
        var currentPercent = total > 0
            ? Math.Round(unlocked / (decimal)total * 100m, 2)
            : await GetUserPercentageAsync(userId, goal.GameId, cancellationToken);

        goal.PercentageGoal = DerivePercentageGoal(
            unlocked,
            goal.GoalAchievements.Count,
            total,
            currentPercent);

        await RefreshGoalEntityAsync(goal, currentPercent, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(userId, id, cancellationToken);
    }

    public async Task<IReadOnlyList<GoalAchievementDetailDto>> GetAchievementsAsync(
        int userId,
        int id,
        CancellationToken cancellationToken = default)
    {
        var goal = await _goals.GetByIdWithAchievementDetailsAsync(userId, id, cancellationToken)
            ?? throw new NotFoundException($"Goal {id} was not found.");

        var achievementIds = goal.GoalAchievements.Select(ga => ga.AchievementId).ToList();
        var unlockedIds = await _usersAchievements.GetUnlockedAchievementIdsAsync(
            userId,
            goal.GameId,
            achievementIds,
            cancellationToken);

        var steamAppId = goal.Game.GameSteamId;
        return goal.GoalAchievements
            .OrderBy(ga => ga.Achievement.Name)
            .Select(ga =>
            {
                var unlocked = unlockedIds.Contains(ga.AchievementId);
                var iconHash = unlocked
                    ? ga.Achievement.ImageUrlUnlock
                    : ga.Achievement.ImageUrlLock;
                return new GoalAchievementDetailDto
                {
                    AchievementId = ga.AchievementId,
                    Name = ga.Achievement.Name,
                    Description = ga.Achievement.Description,
                    Icon = SteamMediaUrls.AppIconUrl(steamAppId, iconHash),
                    GlobalPercentage = ga.Achievement.GlobalPercentage,
                    Status = ga.Status,
                    Unlocked = unlocked
                };
            })
            .ToList();
    }

    public async Task<GoalDto> ReplaceAchievementsAsync(
        int userId,
        int id,
        ReplaceGoalAchievementsRequest request,
        CancellationToken cancellationToken = default)
    {
        var goal = await GetOwnedGoalAsync(userId, id, track: true, cancellationToken);
        if (goal.Status != GoalStatus.Active)
        {
            throw new ConflictException("Only active goals can be edited.");
        }

        var keepIds = (request.AchievementIds ?? [])
            .Where(aid => aid > 0)
            .Distinct()
            .ToHashSet();

        var currentIds = goal.GoalAchievements.Select(ga => ga.AchievementId).ToHashSet();
        if (keepIds.Any(aid => !currentIds.Contains(aid)))
        {
            throw new BadRequestException("Achievements can only be removed from the goal in this view.");
        }

        var currentPercent = await GetUserPercentageAsync(userId, goal.GameId, cancellationToken);
        if (request.PercentageGoal <= currentPercent || request.PercentageGoal > 100m)
        {
            throw new BadRequestException(
                $"Percentage goal must be greater than current progress ({currentPercent}%) and at most 100.");
        }

        var toRemove = goal.GoalAchievements
            .Where(ga => !keepIds.Contains(ga.AchievementId))
            .ToList();
        foreach (var row in toRemove)
        {
            goal.GoalAchievements.Remove(row);
            _goals.RemoveGoalAchievement(row);
        }

        goal.PercentageGoal = request.PercentageGoal;
        await RefreshGoalEntityAsync(goal, currentPercent, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(userId, id, cancellationToken);
    }

    public async Task<GoalDto> AbandonAsync(int userId, int id, CancellationToken cancellationToken = default)
    {
        var goal = await GetOwnedGoalAsync(userId, id, track: true, cancellationToken);
        if (goal.Status != GoalStatus.Active)
        {
            throw new ConflictException("Only active goals can be abandoned.");
        }

        goal.Status = GoalStatus.Abandoned;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(userId, id, cancellationToken);
    }

    public async Task<GoalDto> ReopenAsync(int userId, int id, CancellationToken cancellationToken = default)
    {
        var goal = await GetOwnedGoalAsync(userId, id, track: true, cancellationToken);
        if (goal.Status != GoalStatus.Abandoned)
        {
            throw new ConflictException("Only abandoned goals can be reopened.");
        }

        if (await _goals.HasActiveAsync(userId, goal.GameId, excludeGoalId: goal.Id, cancellationToken))
        {
            throw new ConflictException("This game already has an active goal.");
        }

        goal.Status = GoalStatus.Active;
        goal.CompletedAt = null;

        var currentPercent = await GetUserPercentageAsync(userId, goal.GameId, cancellationToken);
        await RefreshGoalEntityAsync(goal, currentPercent, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(userId, id, cancellationToken);
    }

    public async Task DeleteAsync(int userId, int id, CancellationToken cancellationToken = default)
    {
        var goal = await GetOwnedGoalAsync(userId, id, track: true, cancellationToken);
        _goals.Remove(goal);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task RefreshForGameAsync(int userId, int gameId, CancellationToken cancellationToken = default)
    {
        var goal = await _goals.GetActiveForUpdateAsync(userId, gameId, cancellationToken);
        if (goal is null)
        {
            return;
        }

        var currentPercent = await GetUserPercentageAsync(userId, gameId, cancellationToken);
        await RefreshGoalEntityAsync(goal, currentPercent, cancellationToken, notifyUser: true);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EligibleGameDto>> GetEligibleGamesAsync(
        int userId,
        string? query = null,
        CancellationToken cancellationToken = default)
    {
        var activeGameIds = await _goals.GetActiveGameIdsAsync(userId, cancellationToken);

        var rows = await _usersGames.GetGamesWithAchievementStatsAsync(
            userId,
            excludeCompleted: true,
            nameQuery: query,
            excludeGameIds: activeGameIds,
            take: 50,
            cancellationToken: cancellationToken);

        var games = new List<EligibleGameDto>();
        foreach (var row in rows)
        {
            if (!int.TryParse(row.GameSteamId, out var appId))
            {
                continue;
            }

            games.Add(new EligibleGameDto
            {
                AppId = appId,
                Name = row.Name,
                AchievementsPercentage = row.AchievementsPercentage,
                Icon = SteamMediaUrls.AppIconUrl(row.GameSteamId, row.ImageUrl)
            });
        }

        return games;
    }

    public async Task<GoalsCompletedSeriesDto> GetCompletedSeriesAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var start = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero).AddMonths(-11);

        var completed = await _goals.GetCompletedDatesSinceAsync(userId, start, cancellationToken);

        var labels = new List<string>(12);
        var counts = new List<int>(12);
        for (var i = 0; i < 12; i++)
        {
            var month = start.AddMonths(i);
            labels.Add(month.ToString("MMM yyyy"));
            counts.Add(completed.Count(c => c.Year == month.Year && c.Month == month.Month));
        }

        return new GoalsCompletedSeriesDto
        {
            Labels = labels,
            Counts = counts
        };
    }

    public async Task<IReadOnlyList<UnacknowledgedCompletedGoalDto>> GetUnacknowledgedCompletedAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var acknowledgedAt = await _users.GetGoalsAcknowledgedAtAsync(userId, cancellationToken);
        var goals = await _goals.GetCompletedAfterAsync(userId, acknowledgedAt, cancellationToken);

        if (goals.Count == 0)
        {
            return [];
        }

        var gameIds = goals.Select(g => g.GameId).Distinct().ToList();
        var percentagesByGameId = await _usersGames.GetAchievementPercentagesAsync(
            userId,
            gameIds,
            cancellationToken);
        var unlockedCountsByGameId = await _usersAchievements.GetCountsByGameIdsAsync(
            userId,
            gameIds,
            cancellationToken);

        return goals.Select(goal =>
        {
            var attachedCompleted = goal.GoalAchievements.Count(ga => ga.Status == GoalAchievementStatus.Completed);
            var achievementsEarned = goal.GoalAchievements.Count > 0
                ? attachedCompleted
                : unlockedCountsByGameId.GetValueOrDefault(goal.GameId);

            return new UnacknowledgedCompletedGoalDto
            {
                Id = goal.Id,
                GameName = goal.Game?.Name ?? string.Empty,
                AchievementsEarned = achievementsEarned,
                FinalPercentage = percentagesByGameId.GetValueOrDefault(goal.GameId)
            };
        }).ToList();
    }

    public async Task AcknowledgeCompletedAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetTrackedByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException($"User {userId} was not found.");

        user.ShowGoals = false;
        user.GoalsAcknowledgedAt = DateTimeOffset.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task ValidateLockedAchievementsAsync(
        int userId,
        int gameId,
        IReadOnlyList<int> achievementIds,
        CancellationToken cancellationToken)
    {
        if (achievementIds.Count == 0)
        {
            return;
        }

        var achievements = await _achievements.GetIdentityRowsAsync(achievementIds, cancellationToken);

        if (achievements.Count != achievementIds.Count)
        {
            throw new NotFoundException("One or more achievements were not found.");
        }

        if (achievements.Any(a => a.GameId != gameId))
        {
            throw new BadRequestException("All achievements must belong to the selected game.");
        }

        var unlockedIds = await _usersAchievements.GetUnlockedAchievementIdsAsync(
            userId,
            gameId,
            achievementIds,
            cancellationToken);

        if (unlockedIds.Count > 0)
        {
            throw new BadRequestException("Only locked achievements can be added to a goal.");
        }
    }

    private async Task RefreshGoalEntityAsync(
        Goal goal,
        decimal currentPercent,
        CancellationToken cancellationToken,
        bool notifyUser = false)
    {
        if (goal.Status != GoalStatus.Active)
        {
            return;
        }

        var achievementIds = goal.GoalAchievements.Select(ga => ga.AchievementId).ToList();
        IReadOnlySet<int> unlockedIds = new HashSet<int>();
        if (achievementIds.Count > 0)
        {
            unlockedIds = await _usersAchievements.GetUnlockedAchievementIdsAsync(
                goal.UserId,
                goal.GameId,
                achievementIds,
                cancellationToken);
        }

        foreach (var row in goal.GoalAchievements)
        {
            if (unlockedIds.Contains(row.AchievementId))
            {
                row.Status = GoalAchievementStatus.Completed;
            }
            else if (row.Status != GoalAchievementStatus.Completed)
            {
                row.Status = GoalAchievementStatus.Pending;
            }
        }

        var total = goal.GoalAchievements.Count;
        var missing = goal.GoalAchievements.Count(ga => ga.Status == GoalAchievementStatus.Pending);
        goal.AchievementsMissing = missing;

        var percentReached = currentPercent >= goal.PercentageGoal;
        var allAttachedDone = total > 0 && missing == 0;
        if (percentReached || allAttachedDone)
        {
            goal.Status = GoalStatus.Completed;
            goal.CompletedAt = DateTimeOffset.UtcNow;

            if (notifyUser)
            {
                var user = await _users.GetTrackedByIdAsync(goal.UserId, cancellationToken);
                if (user is not null)
                {
                    user.ShowGoals = true;
                }
            }
        }
    }

    private async Task<decimal> GetUserPercentageAsync(int userId, int gameId, CancellationToken cancellationToken)
    {
        return await _usersGames.GetAchievementPercentageAsync(userId, gameId, cancellationToken) ?? 0m;
    }

    private async Task<(int Unlocked, int Total)> GetUserAchievementCountsAsync(
        int userId,
        int gameId,
        CancellationToken cancellationToken)
    {
        var total = await _achievements.CountForGameAsync(gameId, cancellationToken);
        var unlocked = await _usersAchievements.CountForGameAsync(userId, gameId, cancellationToken);
        return (unlocked, total);
    }

    /// <summary>
    /// Same formula as the goals UI: (unlocked + attached) / total * 100.
    /// Falls back to just above current progress when that would not be valid.
    /// </summary>
    private static decimal DerivePercentageGoal(int unlocked, int selected, int total, decimal currentPercent)
    {
        decimal derived;
        if (total <= 0 || selected <= 0)
        {
            derived = Math.Min(100m, Math.Round(currentPercent + 0.01m, 2));
        }
        else
        {
            derived = Math.Min(100m, Math.Round((unlocked + selected) / (decimal)total * 100m, 2));
        }

        if (derived <= currentPercent)
        {
            derived = Math.Min(100m, Math.Round(currentPercent + 0.01m, 2));
            if (derived <= currentPercent)
            {
                derived = 100m;
            }
        }

        return derived;
    }

    private async Task<Goal> GetOwnedGoalAsync(int userId, int id, bool track, CancellationToken cancellationToken)
    {
        var goal = await _goals.GetByIdForUserAsync(userId, id, track, cancellationToken);
        return goal ?? throw new NotFoundException($"Goal {id} was not found.");
    }

    private static GoalDto Map(Goal goal)
    {
        _ = int.TryParse(goal.Game?.GameSteamId, out var appId);
        return new GoalDto
        {
            Id = goal.Id,
            AppId = appId,
            GameName = goal.Game?.Name ?? string.Empty,
            GameIcon = goal.Game is null
                ? null
                : SteamMediaUrls.AppIconUrl(goal.Game.GameSteamId, goal.Game.ImageUrl),
            PercentageGoal = goal.PercentageGoal,
            AchievementsMissing = goal.AchievementsMissing,
            AchievementsTotal = goal.GoalAchievements?.Count ?? 0,
            Status = goal.Status,
            CreatedAt = goal.CreatedAt,
            CompletedAt = goal.CompletedAt,
            AchievementIds = goal.GoalAchievements?.Select(ga => ga.AchievementId).ToList() ?? []
        };
    }
}
