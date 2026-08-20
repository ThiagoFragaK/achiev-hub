using achiev_hub.Server.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace achiev_hub.Server.Data.Configurations;

public class GameConfiguration : IEntityTypeConfiguration<Game>
{
    public void Configure(EntityTypeBuilder<Game> builder)
    {
        builder.ToTable("games");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.ImageUrl)
            .HasMaxLength(500);

        builder.Property(e => e.GameSteamId)
            .HasMaxLength(100);

        builder.Property(e => e.PlayTime)
            .HasDefaultValue(0);

        builder.HasIndex(e => e.GameSteamId)
            .IsUnique()
            .HasFilter("\"GameSteamId\" IS NOT NULL");

        builder.HasMany(e => e.Achievements)
            .WithOne(a => a.Game)
            .HasForeignKey(a => a.GameId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.UsersGames)
            .WithOne(ug => ug.Game)
            .HasForeignKey(ug => ug.GameId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Goals)
            .WithOne(g => g.Game)
            .HasForeignKey(g => g.GameId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.UsersAchievements)
            .WithOne(ua => ua.Game)
            .HasForeignKey(ua => ua.GameId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
