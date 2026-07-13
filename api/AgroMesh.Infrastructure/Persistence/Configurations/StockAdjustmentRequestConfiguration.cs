using AgroMesh.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgroMesh.Infrastructure.Persistence.Configurations;

public class StockAdjustmentRequestConfiguration : IEntityTypeConfiguration<StockAdjustmentRequest>
{
    public void Configure(EntityTypeBuilder<StockAdjustmentRequest> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.FarmId).IsRequired().HasMaxLength(50);
        builder.Property(r => r.InventoryItemId).IsRequired().HasMaxLength(50);
        builder.Property(r => r.RequestedQuantity).IsRequired().HasColumnType("decimal(14,3)");
        // Stored as a string, same reasoning as User.Role — self-describing on a manual SELECT.
        builder.Property(r => r.Status).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(r => r.Notes).IsRequired().HasMaxLength(500);
        // Unconstrained string columns, no FK to Users — same convention as Farm.OwnerId.
        builder.Property(r => r.RequestedByUserId).IsRequired().HasMaxLength(100);
        builder.Property(r => r.ReviewedByUserId).HasMaxLength(100);
        builder.Property(r => r.ReviewNotes).HasMaxLength(500);

        builder.HasIndex(r => r.FarmId);
        // The queue query's actual access pattern (farm + status).
        builder.HasIndex(r => new { r.FarmId, r.Status });
        builder.HasIndex(r => r.InventoryItemId);
    }
}
