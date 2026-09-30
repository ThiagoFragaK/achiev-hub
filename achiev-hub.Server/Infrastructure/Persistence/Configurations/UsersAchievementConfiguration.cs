using achiev_hub.Server.Domain.Entities;
using achiev_hub.Server.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace achiev_hub.Server.Infrastructure.Persistence.Configurations;

public class UsersAchievementConfiguration : IEntityTypeConfiguration<UsersAchievement>
{
    public void Configure(EntityTypeBuilder<UsersAchievement> builder)
    {
        builder.ToTable("users_achievements");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.UnlockDate)
            .HasColumnName("unlock_date");

        builder.HasIndex(e => new { e.UserId, e.AchievementId }).IsUnique();
    }
}
