using achiev_hub.Server.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace achiev_hub.Server.Data.Configurations;

public class GoalAchievementConfiguration : IEntityTypeConfiguration<GoalAchievement>
{
    public void Configure(EntityTypeBuilder<GoalAchievement> builder)
    {
        builder.ToTable("goal_achievements");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Status)
            .HasConversion<int>()
            .HasDefaultValue(GoalAchievementStatus.Pending);

        builder.HasIndex(e => new { e.GoalId, e.AchievementId }).IsUnique();
    }
}
