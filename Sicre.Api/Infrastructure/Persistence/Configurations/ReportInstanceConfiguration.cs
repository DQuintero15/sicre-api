using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sicre.Api.Domain.Entities;

namespace Sicre.Api.Infrastructure.Persistence.Configurations;

public class ReportInstanceConfiguration : IEntityTypeConfiguration<ReportInstance>
{
    public void Configure(EntityTypeBuilder<ReportInstance> builder)
    {
        builder.ToTable("report_instances", "reports");
        builder.HasKey(ri => ri.Id);

        builder.Property(ri => ri.PeriodName).IsRequired().HasMaxLength(100);
        builder.Property(ri => ri.DelayReason).HasColumnType("text");
        builder.Property(ri => ri.Observations).HasColumnType("text");
        builder.Property(ri => ri.ManualActivationReason).HasColumnType("text");
        builder.Property(ri => ri.DueDateOverrideReason).HasColumnType("text");
        builder.Property(ri => ri.PeriodStart).HasColumnType("date");
        builder.Property(ri => ri.PeriodEnd).HasColumnType("date");
        builder.Property(ri => ri.DueDate).HasColumnType("date").IsRequired();
        builder.Property(ri => ri.EventDate).HasColumnType("date").IsRequired(false);
        builder.Property(ri => ri.SentDate).HasColumnType("timestamptz").IsRequired(false);
        builder
            .Property(ri => ri.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasColumnType("varchar(50)");
        builder
            .Property(ri => ri.CreatedAt)
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(ri => ri.ActivatedAt).HasColumnType("timestamptz").IsRequired(false);
        builder.Property(ri => ri.UpdatedAt).HasColumnType("timestamptz").IsRequired(false);

        builder
            .HasOne(ri => ri.Report)
            .WithMany(r => r.Instances)
            .HasForeignKey(ri => ri.ReportId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne(ri => ri.ResponsibleUser)
            .WithMany()
            .HasForeignKey(ri => ri.ResponsibleUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne(ri => ri.SupervisorUser)
            .WithMany()
            .HasForeignKey(ri => ri.SupervisorUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne(ri => ri.CreatedByUser)
            .WithMany()
            .HasForeignKey(ri => ri.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne(ri => ri.ActivatedByUser)
            .WithMany()
            .HasForeignKey(ri => ri.ActivatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne(ri => ri.UpdatedByUser)
            .WithMany()
            .HasForeignKey(ri => ri.UpdatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasMany(ri => ri.Notifications)
            .WithOne(n => n.ReportInstance)
            .HasForeignKey(n => n.ReportInstanceId)
            .OnDelete(DeleteBehavior.Cascade);
        builder
            .HasMany(ri => ri.Reversions)
            .WithOne(r => r.ReportInstance)
            .HasForeignKey(r => r.ReportInstanceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasIndex(ri => new
            {
                ri.ReportId,
                ri.PeriodYear,
                ri.PeriodMonth,
            })
            .IsUnique()
            .HasFilter("\"EventDate\" IS NULL")
            .HasDatabaseName("IX_ReportInstances_Unique_Automatic");
        builder
            .HasIndex(ri => new { ri.ReportId, ri.EventDate })
            .IsUnique()
            .HasFilter("\"EventDate\" IS NOT NULL")
            .HasDatabaseName("IX_ReportInstances_Unique_Manual");
        builder
            .HasIndex(ri => new { ri.Status, ri.DueDate })
            .HasDatabaseName("IX_ReportInstances_Status_DueDate");
        builder
            .HasIndex(ri => new
            {
                ri.ResponsibleUserId,
                ri.Status,
                ri.DueDate,
            })
            .HasDatabaseName("IX_ReportInstances_Responsible_Status_Due");
        builder
            .HasIndex(ri => new { ri.SupervisorUserId, ri.Status })
            .HasDatabaseName("IX_ReportInstances_Supervisor_Status");
        builder
            .HasIndex(ri => new
            {
                ri.PeriodYear,
                ri.PeriodMonth,
                ri.ReportId,
            })
            .HasDatabaseName("IX_ReportInstances_Period_Report");
        builder.HasIndex(ri => ri.DueDate).HasDatabaseName("IX_ReportInstances_DueDate");
    }
}
