using AgroMesh.Domain.Entities;
using AgroMesh.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgroMesh.Infrastructure.Persistence.Configurations;

public class BlockConfiguration : IEntityTypeConfiguration<Block>
{
    public void Configure(EntityTypeBuilder<Block> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.FarmId).IsRequired().HasMaxLength(50);
        builder.Property(s => s.Name).IsRequired().HasMaxLength(200);
        builder.Property(s => s.Hectares).IsRequired();
        builder.Property(s => s.Status).IsRequired();
        builder.Property(s => s.BoundaryGeoJson).HasColumnType("text");
        builder.Property(s => s.BoundarySource);
        builder.Property(s => s.CentroidLat);
        builder.Property(s => s.CentroidLng);
        builder.Property(s => s.TreeCount);
        builder.Property(s => s.PlantingYear);

        builder.Property(s => s.Crops)
               .HasColumnType("jsonb")
               .HasConversion(
                   v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                   v => System.Text.Json.JsonSerializer.Deserialize<List<BlockCrop>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new());

        builder.HasIndex(s => s.FarmId);
        builder.HasIndex(s => new { s.FarmId, s.Status });
    }
}
