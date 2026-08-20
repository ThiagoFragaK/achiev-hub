using achiev_hub.Server.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace achiev_hub.Server.Data.Configurations;

public class GoalConfiguration : IEntityTypeConfiguration<Goal>
{
    public void Configure(EntityTypeBuilder<Goal> builder)
    {
        builder.ToTable("goals");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.PercentageGoal)
            .HasPrecision(5, 2);

        builder.Property(e => e.AchievementsMissing)
            .HasDefaultValue(0);

        builder.HasMany(e => e.GoalAchievements)
            .WithOne(ga => ga.Goal)
            .HasForeignKey(ga => ga.GoalId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
