using AgroMesh.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgroMesh.Infrastructure.Persistence.Configurations;

public class BoundarySubmissionConfiguration : IEntityTypeConfiguration<BoundarySubmission>
{
    public void Configure(EntityTypeBuilder<BoundarySubmission> builder)
    {
        builder.HasKey(b => b.Id);
        builder.Property(b => b.SectionId).IsRequired().HasMaxLength(50);
        builder.Property(b => b.OperationId).IsRequired().HasMaxLength(100);
        builder.Property(b => b.SubmissionType).IsRequired();
        builder.Property(b => b.RawPointsJson).HasColumnType("text");
        builder.Property(b => b.ComputedGeoJson).IsRequired().HasColumnType("text");
        builder.Property(b => b.ComputedHectares).IsRequired();

        builder.HasIndex(b => b.OperationId).IsUnique();
        builder.HasIndex(b => b.SectionId);
    }
}
