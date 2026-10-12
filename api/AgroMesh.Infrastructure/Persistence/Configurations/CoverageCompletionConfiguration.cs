using AgroMesh.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgroMesh.Infrastructure.Persistence.Configurations;

public class CoverageCompletionConfiguration : IEntityTypeConfiguration<CoverageCompletion>
{
    public void Configure(EntityTypeBuilder<CoverageCompletion> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.FarmId).IsRequired().HasMaxLength(50);
        builder.Property(c => c.BlockId).IsRequired().HasMaxLength(50);
        builder.Property(c => c.ActivityType).IsRequired().HasConversion<string>().HasMaxLength(40);
        builder.Property(c => c.Reason).HasMaxLength(500);
        builder.Property(c => c.CompletedByUserId).IsRequired().HasMaxLength(100);
        builder.HasIndex(c => new { c.BlockId, c.ActivityType });
    }
}
