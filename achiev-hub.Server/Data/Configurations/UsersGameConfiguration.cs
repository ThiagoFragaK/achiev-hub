using achiev_hub.Server.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace achiev_hub.Server.Data.Configurations;

public class UsersGameConfiguration : IEntityTypeConfiguration<UsersGame>
{
    public void Configure(EntityTypeBuilder<UsersGame> builder)
    {
        builder.ToTable("users_games");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.AchievementsPercentage)
            .HasPrecision(5, 2)
            .HasDefaultValue(0);

        builder.HasIndex(e => new { e.UserId, e.GameId }).IsUnique();
    }
}
