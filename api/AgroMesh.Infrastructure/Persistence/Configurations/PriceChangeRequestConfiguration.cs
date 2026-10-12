using AgroMesh.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgroMesh.Infrastructure.Persistence.Configurations;

public class PriceChangeRequestConfiguration : IEntityTypeConfiguration<PriceChangeRequest>
{
    public void Configure(EntityTypeBuilder<PriceChangeRequest> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.FarmId).IsRequired().HasMaxLength(50);
        builder.Property(r => r.InventoryItemId).IsRequired().HasMaxLength(50);
        builder.Property(r => r.PackLabel).IsRequired().HasMaxLength(40);
        builder.Property(r => r.PackSize).IsRequired().HasColumnType("decimal(14,3)");
        builder.Property(r => r.PricePerPack).IsRequired().HasColumnType("decimal(14,2)");
        builder.Property(r => r.Notes).HasMaxLength(500);
        builder.Property(r => r.Status).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(r => r.RequestedByUserId).IsRequired().HasMaxLength(100);
        builder.Property(r => r.ReviewedByUserId).HasMaxLength(100);
        builder.Property(r => r.ReviewNotes).HasMaxLength(500);
        builder.Ignore(r => r.PricePerUnit);

        builder.HasIndex(r => new { r.FarmId, r.Status });
        builder.HasIndex(r => r.InventoryItemId);
    }
}
