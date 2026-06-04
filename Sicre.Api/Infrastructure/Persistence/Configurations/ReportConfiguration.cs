using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Sicre.Api.Domain.Entities;
using Sicre.Api.Domain.Enums;

namespace Sicre.Api.Infrastructure.Persistence.Configurations;

public class ReportConfiguration : IEntityTypeConfiguration<Report>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public void Configure(EntityTypeBuilder<Report> builder)
    {
        builder.ToTable("reports", "reports");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Code).IsRequired().HasMaxLength(50);
        builder.Property(r => r.Name).IsRequired().HasMaxLength(200);
        builder.Property(r => r.LegalBasis).HasColumnType("text");
        builder.Property(r => r.Description).HasColumnType("text");
        builder.Property(r => r.InstructionsUrl).HasColumnType("text");
        builder.Property(r => r.TemplateFileUrl).HasColumnType("text");
        // NotificationEmails is a plain comma-separated string — store as text, not jsonb
        builder.Property(r => r.NotificationEmails).HasColumnType("text");
        builder.Property(r => r.DueDateDatesDefinition).HasColumnType("jsonb");
        builder.Property(r => r.OriginalDueDateText).HasColumnType("text");
        builder.Property(r => r.IsActive).HasDefaultValue(true);
        builder.Property(r => r.StartDate).HasColumnType("date").IsRequired();
        builder.Property(r => r.EndDate).HasColumnType("date").IsRequired(false);

        builder
            .Property(r => r.Frequency)
            .IsRequired()
            .HasConversion<string>()
            .HasColumnType("varchar(50)");
        builder
            .Property(r => r.GenerationMode)
            .IsRequired()
            .HasConversion<string>()
            .HasColumnType("varchar(50)");
        builder
            .Property(r => r.DueDateRuleType)
            .IsRequired()
            .HasConversion<string>()
            .HasColumnType("varchar(50)");

        // FormatTypes: serialize enum names (not integers) to jsonb
        var formatTypesConverter = new ValueConverter<List<ReportFormatType>, string>(
            v => JsonSerializer.Serialize(v, JsonOptions),
            v =>
                JsonSerializer.Deserialize<List<ReportFormatType>>(v, JsonOptions)
                ?? new List<ReportFormatType>()
        );
        builder
            .Property(r => r.FormatTypes)
            .IsRequired()
            .HasConversion(formatTypesConverter)
            .HasColumnType("jsonb");
        builder.Property(r => r.AlertEarlyDays).HasDefaultValue(15);
        builder.Property(r => r.AlertFollowUpDays).HasDefaultValue(5);
        builder.Property(r => r.AlertCriticalDays).HasDefaultValue(0);
        builder
            .Property(r => r.CreatedAt)
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(r => r.UpdatedAt).HasColumnType("timestamptz").IsRequired(false);

        builder
            .HasOne(r => r.ControlEntity)
            .WithMany(ce => ce.Reports)
            .HasForeignKey(r => r.ControlEntityId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne(r => r.Process)
            .WithMany()
            .HasForeignKey(r => r.ProcessId)
            .OnDelete(DeleteBehavior.SetNull);
        builder
            .HasOne(r => r.Branch)
            .WithMany(b => b.Reports)
            .HasForeignKey(r => r.BranchId)
            .OnDelete(DeleteBehavior.SetNull);
        builder
            .HasOne(r => r.SenderResponsibleUser)
            .WithMany()
            .HasForeignKey(r => r.SenderResponsibleUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne(r => r.EntityUploadResponsibleUser)
            .WithMany()
            .HasForeignKey(r => r.EntityUploadResponsibleUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne(r => r.FollowUpLeaderUser)
            .WithMany()
            .HasForeignKey(r => r.FollowUpLeaderUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne(r => r.CreatedByUser)
            .WithMany()
            .HasForeignKey(r => r.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne(r => r.UpdatedByUser)
            .WithMany()
            .HasForeignKey(r => r.UpdatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasIndex(r => new
            {
                r.Code,
                r.ControlEntityId,
                r.BranchId,
            })
            .IsUnique()
            .HasDatabaseName("IX_Reports_Code_ControlEntity_Branch");
        builder
            .HasIndex(r => new
            {
                r.ControlEntityId,
                r.IsActive,
                r.Frequency,
            })
            .HasDatabaseName("IX_Reports_ControlEntity_Active_Frequency");
        builder.HasIndex(r => r.IsActive).HasDatabaseName("IX_Reports_IsActive");
        builder.HasIndex(r => r.Frequency).HasDatabaseName("IX_Reports_Frequency");
        builder.HasIndex(r => r.GenerationMode).HasDatabaseName("IX_Reports_GenerationMode");
    }
}
