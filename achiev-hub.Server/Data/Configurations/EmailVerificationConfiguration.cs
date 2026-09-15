using achiev_hub.Server.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace achiev_hub.Server.Data.Configurations;

public class EmailVerificationConfiguration : IEntityTypeConfiguration<EmailVerification>
{
    public void Configure(EntityTypeBuilder<EmailVerification> builder)
    {
        builder.ToTable("email_verifications");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.CodeHash)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(e => e.ExpiresAt).IsRequired();
        builder.Property(e => e.LastSentAt).IsRequired();
        builder.Property(e => e.VerifiedAt);

        builder.Property(e => e.ConsumeTokenHash)
            .HasMaxLength(512);

        builder.Property(e => e.ConsumeTokenExpiresAt);

        builder.HasIndex(e => e.Email).IsUnique();
    }
}
