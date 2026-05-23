using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sicre.Api.Domain.Entities;

namespace Sicre.Api.Infrastructure.Persistence.Configurations;

public class PasswordResetRequestConfiguration : IEntityTypeConfiguration<PasswordResetRequest>
{
    public void Configure(EntityTypeBuilder<PasswordResetRequest> builder)
    {
        builder.ToTable("password_reset_requests");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Email).IsRequired().HasMaxLength(256);
        builder.Property(r => r.TokenHash).IsRequired().HasMaxLength(500);
        builder.Property(r => r.IpAddress).HasMaxLength(45);
        builder.Property(r => r.UserAgent).HasMaxLength(500);
        builder
            .Property(r => r.RequestedAt)
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(r => r.ExpiresAt).HasColumnType("timestamptz");
        builder.Property(r => r.UsedAt).HasColumnType("timestamptz").IsRequired(false);
        builder.HasIndex(r => r.TokenHash).HasDatabaseName("IX_PasswordResetRequests_Token");
        builder
            .HasIndex(r => new { r.Email, r.UsedAt })
            .HasDatabaseName("IX_PasswordResetRequests_Email_Used");
    }
}
