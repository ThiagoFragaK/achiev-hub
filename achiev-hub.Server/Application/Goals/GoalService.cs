using achiev_hub.Server.Application.Common;
using achiev_hub.Server.Application.Common.Interfaces;
using achiev_hub.Server.Application.Goals.Interfaces;
using achiev_hub.Server.Domain.Entities;
using achiev_hub.Server.Domain.Enums;
using achiev_hub.Server.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace achiev_hub.Server.Application.Goals;

public class GoalService : IGoalService
{
    private readonly ApplicationDbContext _db;

    public GoalService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<GoalDto>> GetForUserAsync(
        int userId,
        GoalStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Goals
            .AsNoTracking()
            .Include(g => g.Game)
            .Include(g => g.GoalAchievements)
            .Where(g => g.UserId == userId);

        if (status.HasValue)
        {
            query = query.Where(g => g.Status == status.Value);
        }

        var goals = await query
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync(cancellationToken);

        return goals.Select(Map).ToList();
    }

    public async Task<GoalDto> GetByIdAsync(int userId, int id, CancellationToken cancellationToken = default)
    {
        return Map(await GetOwnedGoalAsync(userId, id, track: false, cancellationToken));
    }

    public async Task<GoalDto?> GetActiveByAppIdAsync(int userId, int appId, CancellationToken cancellationToken = default)
    {
        var steamAppId = appId.ToString();
        var goal = await _db.Goals
            .AsNoTracking()
            .Include(g => g.Game)
            .Include(g => g.GoalAchievements)
            .FirstOrDefaultAsync(
                g => g.UserId == userId
                     && g.Status == GoalStatus.Active
                     && g.Game.GameSteamId == steamAppId,
                cancellationToken);

        return goal is null ? null : Map(goal);
    }

    public async Task<GoalDto> CreateAsync(int userId, CreateGoalRequest request, CancellationToken cancellationToken = default)
    {
        if (request.AppId <= 0)
        {
            throw new BadRequestException("AppId is required.");
        }

        var steamAppId = request.AppId.ToString();
        var game = await _db.Games
            .FirstOrDefaultAsync(g => g.GameSteamId == steamAppId, cancellationToken)
            ?? throw new NotFoundException($"Game {request.AppId} was not found.");

        var usersGame = await _db.UsersGames
            .FirstOrDefaultAsync(ug => ug.UserId == userId && ug.GameId == game.Id, cancellationToken)
            ?? throw new NotFoundException($"Game {request.AppId} is not in your library.");

        if (usersGame.AchievementsPercentage >= 100m)
        {
            throw new BadRequestException("Cannot create a goal for a fully completed game.");
        }

        if (request.PercentageGoal <= usersGame.AchievementsPercentage || request.PercentageGoal > 100m)
        {
            throw new BadRequestException(
                $"Percentage goal must be greater than current progress ({usersGame.AchievementsPercentage}%) and at most 100.");
        }

        var hasActive = await _db.Goals.AnyAsync(
            g => g.UserId == userId && g.GameId == game.Id && g.Status == GoalStatus.Active,
            cancellationToken);
        if (hasActive)
        {
            throw new ConflictException("This game already has an active goal.");
        }

        var achievementIds = (request.AchievementIds ?? [])
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        await ValidateLockedAchievementsAsync(userId, game.Id, achievementIds, cancellationToken);

        var goal = new Goal
        {
            UserId = userId,
            GameId = game.Id,
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

        _db.Goals.Add(goal);
        await _db.SaveChangesAsync(cancellationToken);

        await RefreshGoalEntityAsync(goal, usersGame.AchievementsPercentage, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(userId, goal.Id, cancellationToken);
    }

    public async Task<GoalDto> UpdateAsync(int userId, int id, UpdateGoalRequest request, CancellationToken cancellationToken = default)
    {
        var goal = await GetOwnedGoalAsync(userId, id, track: true, cancellationToken);
        if (goal.Status != GoalStatus.Active)
        {
            throw new ConflictException("Only active goals can be updated.");
        }

        var usersGame = await _db.UsersGames
            .AsNoTracking()
            .FirstOrDefaultAsync(ug => ug.UserId == userId && ug.GameId == goal.GameId, cancellationToken);

        var currentPercent = usersGame?.AchievementsPercentage ?? 0m;
        if (request.PercentageGoal <= currentPercent || request.PercentageGoal > 100m)
        {
            throw new BadRequestException(
                $"Percentage goal must be greater than current progress ({currentPercent}%) and at most 100.");
        }

        goal.PercentageGoal = request.PercentageGoal;
        await RefreshGoalEntityAsync(goal, currentPercent, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
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

        var currentPercent = await GetUserPercentageAsync(userId, goal.GameId, cancellationToken);
        await RefreshGoalEntityAsync(goal, currentPercent, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(userId, id, cancellationToken);
    }

    public async Task<IReadOnlyList<GoalAchievementDetailDto>> GetAchievementsAsync(
        int userId,
        int id,
        CancellationToken cancellationToken = default)
    {
        var goal = await _db.Goals
            .AsNoTracking()
            .Include(g => g.Game)
            .Include(g => g.GoalAchievements)
            .ThenInclude(ga => ga.Achievement)
            .FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId, cancellationToken)
            ?? throw new NotFoundException($"Goal {id} was not found.");

        var achievementIds = goal.GoalAchievements.Select(ga => ga.AchievementId).ToList();
        var unlockedIds = achievementIds.Count == 0
            ? new HashSet<int>()
            : (await _db.UsersAchievements
                    .AsNoTracking()
                    .Where(ua => ua.UserId == userId && ua.GameId == goal.GameId && achievementIds.Contains(ua.AchievementId))
                    .Select(ua => ua.AchievementId)
                    .ToListAsync(cancellationToken))
                .ToHashSet();

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
            _db.GoalAchievements.Remove(row);
        }

        goal.PercentageGoal = request.PercentageGoal;
        await RefreshGoalEntityAsync(goal, currentPercent, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
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
        await _db.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(userId, id, cancellationToken);
    }

    public async Task DeleteAsync(int userId, int id, CancellationToken cancellationToken = default)
    {
        var goal = await GetOwnedGoalAsync(userId, id, track: true, cancellationToken);
        _db.Goals.Remove(goal);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task RefreshForGameAsync(int userId, int gameId, CancellationToken cancellationToken = default)
    {
        var goal = await _db.Goals
            .Include(g => g.GoalAchievements)
            .FirstOrDefaultAsync(
                g => g.UserId == userId && g.GameId == gameId && g.Status == GoalStatus.Active,
                cancellationToken);

        if (goal is null)
        {
            return;
        }

        var currentPercent = await GetUserPercentageAsync(userId, gameId, cancellationToken);
        await RefreshGoalEntityAsync(goal, currentPercent, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EligibleGameDto>> GetEligibleGamesAsync(
        int userId,
        string? query = null,
        CancellationToken cancellationToken = default)
    {
        var activeGameIds = await _db.Goals
            .AsNoTracking()
            .Where(g => g.UserId == userId && g.Status == GoalStatus.Active)
            .Select(g => g.GameId)
            .ToListAsync(cancellationToken);

        var activeSet = activeGameIds.ToHashSet();

        var rowsQuery = _db.UsersGames
            .AsNoTracking()
            .Where(ug =>
                ug.UserId == userId
                && ug.AchievementsPercentage < 100m
                && ug.Game.HasCommunityVisibleStats == true
                && ug.Game.Achievements.Any()
                && !activeSet.Contains(ug.GameId));

        if (!string.IsNullOrWhiteSpace(query))
        {
            var q = query.Trim().ToLowerInvariant();
            rowsQuery = rowsQuery.Where(ug => ug.Game.Name.ToLower().Contains(q));
        }

        var rows = await rowsQuery
            .OrderBy(ug => ug.Game.Name)
            .Select(ug => new
            {
                ug.Game.GameSteamId,
                ug.Game.Name,
                ug.Game.ImageUrl,
                ug.AchievementsPercentage
            })
            .Take(50)
            .ToListAsync(cancellationToken);

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

        var completed = await _db.Goals
            .AsNoTracking()
            .Where(g =>
                g.UserId == userId
                && g.Status == GoalStatus.Completed
                && g.CompletedAt != null
                && g.CompletedAt >= start)
            .Select(g => g.CompletedAt!.Value)
            .ToListAsync(cancellationToken);

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

        var achievements = await _db.Achievements
            .AsNoTracking()
            .Where(a => achievementIds.Contains(a.Id))
            .Select(a => new { a.Id, a.GameId })
            .ToListAsync(cancellationToken);

        if (achievements.Count != achievementIds.Count)
        {
            throw new NotFoundException("One or more achievements were not found.");
        }

        if (achievements.Any(a => a.GameId != gameId))
        {
            throw new BadRequestException("All achievements must belong to the selected game.");
        }

        var unlockedIds = await _db.UsersAchievements
            .AsNoTracking()
            .Where(ua => ua.UserId == userId && ua.GameId == gameId && achievementIds.Contains(ua.AchievementId))
            .Select(ua => ua.AchievementId)
            .ToListAsync(cancellationToken);

        if (unlockedIds.Count > 0)
        {
            throw new BadRequestException("Only locked achievements can be added to a goal.");
        }
    }

    private async Task RefreshGoalEntityAsync(Goal goal, decimal currentPercent, CancellationToken cancellationToken)
    {
        if (goal.Status != GoalStatus.Active)
        {
            return;
        }

        var achievementIds = goal.GoalAchievements.Select(ga => ga.AchievementId).ToList();
        HashSet<int> unlockedIds = [];
        if (achievementIds.Count > 0)
        {
            unlockedIds = (await _db.UsersAchievements
                    .AsNoTracking()
                    .Where(ua => ua.UserId == goal.UserId && ua.GameId == goal.GameId && achievementIds.Contains(ua.AchievementId))
                    .Select(ua => ua.AchievementId)
                    .ToListAsync(cancellationToken))
                .ToHashSet();
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
        }
    }

    private async Task<decimal> GetUserPercentageAsync(int userId, int gameId, CancellationToken cancellationToken)
    {
        var percent = await _db.UsersGames
            .AsNoTracking()
            .Where(ug => ug.UserId == userId && ug.GameId == gameId)
            .Select(ug => (decimal?)ug.AchievementsPercentage)
            .FirstOrDefaultAsync(cancellationToken);
        return percent ?? 0m;
    }

    private async Task<Goal> GetOwnedGoalAsync(int userId, int id, bool track, CancellationToken cancellationToken)
    {
        var query = track
            ? _db.Goals.Include(g => g.Game).Include(g => g.GoalAchievements)
            : _db.Goals.AsNoTracking().Include(g => g.Game).Include(g => g.GoalAchievements);

        var goal = await query.FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId, cancellationToken);
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
