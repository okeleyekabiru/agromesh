using AgroMesh.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgroMesh.Infrastructure.Persistence.Configurations;

public class BlockSatelliteReadingConfiguration : IEntityTypeConfiguration<BlockSatelliteReading>
{
    public void Configure(EntityTypeBuilder<BlockSatelliteReading> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.FarmId).IsRequired().HasMaxLength(50);
        builder.Property(r => r.BlockId).IsRequired().HasMaxLength(50);
        builder.HasIndex(r => new { r.BlockId, r.Date }).IsUnique();
        builder.HasIndex(r => r.FarmId);
    }
}
