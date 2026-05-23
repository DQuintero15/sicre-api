using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sicre.Api.Domain.Entities;

namespace Sicre.Api.Infrastructure.Persistence.Configurations;

public class ReportAttachmentConfiguration : IEntityTypeConfiguration<ReportAttachment>
{
    public void Configure(EntityTypeBuilder<ReportAttachment> builder)
    {
        builder.ToTable("report_attachments", "reports");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.FileName).IsRequired().HasMaxLength(500);
        builder.Property(a => a.MimeType).HasMaxLength(200);
        builder.Property(a => a.GoogleFileId).HasMaxLength(200);
        builder.Property(a => a.WebViewLink).HasColumnType("text");
        builder.Property(a => a.WebContentLink).HasColumnType("text");
        builder
            .Property(a => a.Type)
            .IsRequired()
            .HasConversion<string>()
            .HasColumnType("varchar(50)");
        builder.Property(a => a.IsActive).HasDefaultValue(true);
        builder
            .Property(a => a.CreatedAt)
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder
            .HasOne(a => a.ReportInstance)
            .WithMany(ri => ri.Attachments)
            .HasForeignKey(a => a.ReportInstanceId)
            .OnDelete(DeleteBehavior.Cascade);
        builder
            .HasOne(a => a.UploadedByUser)
            .WithMany()
            .HasForeignKey(a => a.UploadedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasIndex(a => new { a.ReportInstanceId, a.Type })
            .HasDatabaseName("IX_ReportAttachments_Instance_Type");
        builder.HasIndex(a => a.GoogleFileId).HasDatabaseName("IX_ReportAttachments_GoogleFileId");
    }
}
