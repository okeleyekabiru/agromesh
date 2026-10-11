using AgroMesh.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgroMesh.Infrastructure.Persistence.Configurations;

public class CropAllocationRuleConfiguration : IEntityTypeConfiguration<CropAllocationRule>
{
    public void Configure(EntityTypeBuilder<CropAllocationRule> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.FarmId).IsRequired().HasMaxLength(50);
        builder.Property(r => r.Category).IsRequired();
        builder.Property(r => r.BlockId).HasMaxLength(50);

        builder.Property(r => r.Allocation)
               .HasColumnType("jsonb")
               .HasConversion(
                   v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                   v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, double>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new());

        builder.Property(r => r.SeasonMonths)
               .HasColumnType("jsonb")
               .HasConversion(
                   v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                   v => System.Text.Json.JsonSerializer.Deserialize<List<int>>(v, (System.Text.Json.JsonSerializerOptions?)null));

        builder.HasIndex(r => new { r.FarmId, r.Category });
    }
}
