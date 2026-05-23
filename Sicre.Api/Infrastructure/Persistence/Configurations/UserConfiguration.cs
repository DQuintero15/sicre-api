using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sicre.Api.Domain.Entities;

namespace Sicre.Api.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.Property(u => u.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(u => u.LastName).IsRequired().HasMaxLength(100);
        builder.Property(u => u.TwoFactorSecret).HasMaxLength(200);
        builder.Property(u => u.IsActive).HasDefaultValue(true);
        builder.Property(u => u.HasChangedDefaultPassword).HasDefaultValue(false);
        builder
            .Property(u => u.CreatedAt)
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder
            .HasOne(u => u.Position)
            .WithMany()
            .HasForeignKey(u => u.PositionId)
            .OnDelete(DeleteBehavior.SetNull);
        builder
            .HasOne(u => u.Process)
            .WithMany()
            .HasForeignKey(u => u.ProcessId)
            .OnDelete(DeleteBehavior.SetNull);
        builder
            .HasOne(u => u.Branch)
            .WithMany()
            .HasForeignKey(u => u.BranchId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(u => u.IsActive).HasDatabaseName("IX_Users_IsActive");
        builder.HasIndex(u => u.BranchId).HasDatabaseName("IX_Users_BranchId");
    }
}
