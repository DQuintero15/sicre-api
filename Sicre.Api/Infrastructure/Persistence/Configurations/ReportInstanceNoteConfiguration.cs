using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sicre.Api.Domain.Entities;

namespace Sicre.Api.Infrastructure.Persistence.Configurations;

public class ReportInstanceNoteConfiguration : IEntityTypeConfiguration<ReportInstanceNote>
{
    public void Configure(EntityTypeBuilder<ReportInstanceNote> builder)
    {
        builder.ToTable("report_instance_notes", "reports");
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Content).IsRequired().HasColumnType("text");
        builder.Property(n => n.CreatedAt).HasColumnType("timestamptz").HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.HasIndex(n => n.ReportInstanceId).HasDatabaseName("IX_ReportInstanceNotes_InstanceId");
        builder
            .HasOne(n => n.ReportInstance)
            .WithMany(ri => ri.Notes)
            .HasForeignKey(n => n.ReportInstanceId)
            .OnDelete(DeleteBehavior.Cascade);
        builder
            .HasOne(n => n.CreatedByUser)
            .WithMany()
            .HasForeignKey(n => n.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
