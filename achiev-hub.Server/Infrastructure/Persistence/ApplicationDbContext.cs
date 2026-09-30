using achiev_hub.Server.Domain.Entities;
using achiev_hub.Server.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace achiev_hub.Server.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<EmailVerification> EmailVerifications => Set<EmailVerification>();
    public DbSet<Game> Games => Set<Game>();
    public DbSet<Achievement> Achievements => Set<Achievement>();
    public DbSet<Goal> Goals => Set<Goal>();
    public DbSet<UsersGame> UsersGames => Set<UsersGame>();
    public DbSet<UsersAchievement> UsersAchievements => Set<UsersAchievement>();
    public DbSet<GoalAchievement> GoalAchievements => Set<GoalAchievement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
