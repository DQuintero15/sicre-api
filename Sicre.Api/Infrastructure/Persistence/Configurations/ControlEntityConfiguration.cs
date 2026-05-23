using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sicre.Api.Domain.Entities;

namespace Sicre.Api.Infrastructure.Persistence.Configurations;

public class ControlEntityConfiguration : IEntityTypeConfiguration<ControlEntity>
{
    public void Configure(EntityTypeBuilder<ControlEntity> builder)
    {
        builder.ToTable("control_entities", "organization");
        builder.HasKey(ce => ce.Id);
        builder.Property(ce => ce.Name).IsRequired().HasMaxLength(200);
        builder.Property(ce => ce.Abbreviation).HasMaxLength(20);
        builder.Property(ce => ce.Nit).HasMaxLength(20);
        builder.Property(ce => ce.LegalBasis).HasColumnType("text");
        builder.Property(ce => ce.Website).HasMaxLength(500);
        builder.Property(ce => ce.IsActive).HasDefaultValue(true);
        builder
            .Property(ce => ce.CreatedAt)
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(ce => ce.UpdatedAt).HasColumnType("timestamptz").IsRequired(false);
        builder.HasIndex(ce => ce.Abbreviation).HasDatabaseName("IX_ControlEntities_Abbreviation");
        builder.HasIndex(ce => ce.IsActive).HasDatabaseName("IX_ControlEntities_IsActive");
    }
}
