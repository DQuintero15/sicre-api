using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sicre.Api.Domain.Entities;

namespace Sicre.Api.Infrastructure.Persistence.Configurations;

public class SICRESettingsConfiguration : IEntityTypeConfiguration<SICRESettings>
{
    public void Configure(EntityTypeBuilder<SICRESettings> builder)
    {
        builder.ToTable("sicre_settings");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.GoLiveDate).HasColumnType("date").IsRequired(false);
        builder.Property(s => s.AutoNotify).HasDefaultValue(true);
        builder.Property(s => s.UpdatedAt).HasColumnType("timestamptz").IsRequired(false);
        builder
            .HasOne(s => s.UpdatedByUser)
            .WithMany()
            .HasForeignKey(s => s.UpdatedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
