using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sicre.Api.Domain.Entities;

namespace Sicre.Api.Infrastructure.Persistence.Configurations;

public class GoogleDriveTokenConfiguration : IEntityTypeConfiguration<GoogleDriveToken>
{
    public void Configure(EntityTypeBuilder<GoogleDriveToken> builder)
    {
        builder.ToTable("google_drive_tokens");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.AccessToken).IsRequired().HasColumnType("text");
        builder.Property(t => t.RefreshToken).IsRequired().HasColumnType("text");
        builder.Property(t => t.ExpiresAt).HasColumnType("timestamptz");
        builder.Property(t => t.UpdatedAt).HasColumnType("timestamptz");
    }
}
