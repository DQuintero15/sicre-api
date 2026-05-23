using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sicre.Api.Domain.Entities;

namespace Sicre.Api.Infrastructure.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Title).IsRequired().HasMaxLength(200);
        builder.Property(n => n.Content).IsRequired().HasColumnType("text");
        builder
            .Property(n => n.Type)
            .IsRequired()
            .HasConversion<string>()
            .HasColumnType("varchar(20)");
        builder.Property(n => n.Severity).HasConversion<string>().HasColumnType("varchar(20)");
        builder.Property(n => n.Priority).HasConversion<string>().HasColumnType("varchar(20)");
        builder.Property(n => n.Readed).HasDefaultValue(false);
        builder.Property(n => n.Url).HasMaxLength(500);
        builder
            .Property(n => n.CreatedAt)
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(n => n.SentAt).HasColumnType("timestamptz").IsRequired(false);
        builder.Property(n => n.ReadAt).HasColumnType("timestamptz").IsRequired(false);

        builder
            .HasOne(n => n.User)
            .WithMany(u => u.Notifications)
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasIndex(n => new
            {
                n.UserId,
                n.Readed,
                n.CreatedAt,
            })
            .HasDatabaseName("IX_Notifications_User_Readed_Created");
        builder
            .HasIndex(n => new { n.UserId, n.CreatedAt })
            .HasDatabaseName("IX_Notifications_User_Created");
        builder
            .HasIndex(n => n.ReportInstanceId)
            .HasDatabaseName("IX_Notifications_ReportInstance");
    }
}
