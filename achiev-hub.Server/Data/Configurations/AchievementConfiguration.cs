using achiev_hub.Server.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace achiev_hub.Server.Data.Configurations;

public class AchievementConfiguration : IEntityTypeConfiguration<Achievement>
{
    public void Configure(EntityTypeBuilder<Achievement> builder)
    {
        builder.ToTable("achievements");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Description)
            .HasColumnName("desc")
            .HasMaxLength(1000);

        builder.Property(e => e.ImageUrlLock)
            .HasMaxLength(500);

        builder.Property(e => e.ImageUrlUnlock)
            .HasMaxLength(500);

        builder.Property(e => e.GlobalPercentage)
            .HasPrecision(5, 2);

        builder.HasMany(e => e.UsersAchievements)
            .WithOne(ua => ua.Achievement)
            .HasForeignKey(ua => ua.AchievementId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.GoalAchievements)
            .WithOne(ga => ga.Achievement)
            .HasForeignKey(ga => ga.AchievementId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
