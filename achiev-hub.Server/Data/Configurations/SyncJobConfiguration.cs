using achiev_hub.Server.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace achiev_hub.Server.Data.Configurations;

public class SyncJobConfiguration : IEntityTypeConfiguration<SyncJob>
{
    public void Configure(EntityTypeBuilder<SyncJob> builder)
    {
        builder.ToTable("sync_jobs");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.SteamId)
            .HasMaxLength(64);

        builder.Property(e => e.Type)
            .IsRequired();

        builder.Property(e => e.Status)
            .IsRequired();

        builder.Property(e => e.Cursor)
            .HasDefaultValue(0);

        builder.Property(e => e.ProgressDone)
            .HasDefaultValue(0);

        builder.Property(e => e.ProgressTotal)
            .HasDefaultValue(0);

        builder.Property(e => e.Attempts)
            .HasDefaultValue(0);

        builder.Property(e => e.LastError)
            .HasMaxLength(2000);

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .IsRequired();

        builder.Property(e => e.AvailableAt)
            .IsRequired();

        builder.HasIndex(e => new { e.Status, e.AvailableAt });
        builder.HasIndex(e => new { e.UserId, e.Type, e.Status });

        builder.HasOne(e => e.User)
            .WithMany(u => u.SyncJobs)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
