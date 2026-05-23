using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sicre.Api.Domain.Entities;

namespace Sicre.Api.Infrastructure.Persistence.Configurations;

public class ReportReversionConfiguration : IEntityTypeConfiguration<ReportReversion>
{
    public void Configure(EntityTypeBuilder<ReportReversion> builder)
    {
        builder.ToTable("report_reversions", "reports");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Reason).IsRequired().HasColumnType("text");
        builder
            .Property(r => r.PreviousStatus)
            .IsRequired()
            .HasConversion<string>()
            .HasColumnType("varchar(50)");
        builder
            .Property(r => r.NewStatus)
            .IsRequired()
            .HasConversion<string>()
            .HasColumnType("varchar(50)");
        builder
            .Property(r => r.CreatedAt)
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder
            .HasOne(r => r.CreatedByUser)
            .WithMany()
            .HasForeignKey(r => r.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(r => r.ReportInstanceId).HasDatabaseName("IX_ReportReversions_InstanceId");
    }
}
