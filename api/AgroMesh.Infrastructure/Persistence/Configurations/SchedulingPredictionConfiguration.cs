using AgroMesh.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgroMesh.Infrastructure.Persistence.Configurations;

public class SchedulingPredictionConfiguration : IEntityTypeConfiguration<SchedulingPrediction>
{
    public void Configure(EntityTypeBuilder<SchedulingPrediction> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.FarmId).IsRequired().HasMaxLength(50);
        builder.Property(p => p.BlockId).IsRequired().HasMaxLength(50);
        builder.Property(p => p.ActivityType).IsRequired();
        builder.Property(p => p.PredictedNextDue).IsRequired();
        builder.Property(p => p.Reasoning).IsRequired().HasMaxLength(500);
        builder.Property(p => p.RecommendedChemical).HasMaxLength(200);
        builder.Property(p => p.RecommendedSprayDate);
        builder.Property(p => p.Confidence).IsRequired();
        builder.Property(p => p.ComputedAt).IsRequired();
        builder.Property(p => p.WeatherDataUsed).IsRequired();

        builder.Ignore(p => p.IsStale);

        builder.HasIndex(p => new { p.BlockId, p.ActivityType }).IsUnique();
        builder.HasIndex(p => new { p.FarmId, p.PredictedNextDue });
    }
}
