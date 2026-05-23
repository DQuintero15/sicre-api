using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sicre.Api.Domain.Entities;

namespace Sicre.Api.Infrastructure.Persistence.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");
        builder.HasKey(rt => rt.Id);
        builder.Property(rt => rt.Token).IsRequired().HasMaxLength(500);
        builder.Property(rt => rt.IsRevoked).HasDefaultValue(false);
        builder
            .Property(rt => rt.CreatedAt)
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(rt => rt.ExpiresAt).HasColumnType("timestamptz");
        builder.Ignore(rt => rt.IsExpired);
        builder.Ignore(rt => rt.IsActive);
        builder
            .HasOne(rt => rt.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(rt => rt.Token).IsUnique().HasDatabaseName("IX_RefreshTokens_Token");
        builder
            .HasIndex(rt => new
            {
                rt.UserId,
                rt.IsRevoked,
                rt.ExpiresAt,
            })
            .HasDatabaseName("IX_RefreshTokens_User_Active");
    }
}
