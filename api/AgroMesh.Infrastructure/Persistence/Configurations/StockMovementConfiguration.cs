using AgroMesh.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgroMesh.Infrastructure.Persistence.Configurations;

public class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.FarmId).IsRequired().HasMaxLength(50);
        builder.Property(m => m.InventoryItemId).IsRequired().HasMaxLength(50);
        builder.Property(m => m.Type).IsRequired();
        builder.Property(m => m.Quantity).IsRequired().HasColumnType("decimal(14,3)");
        builder.Property(m => m.ActivityLogId).HasMaxLength(50);
        builder.Property(m => m.TransactionId).HasMaxLength(50);
        builder.Property(m => m.Notes).HasMaxLength(500);
        builder.Property(m => m.Date).IsRequired();

        builder.HasIndex(m => new { m.FarmId, m.Date });
        builder.HasIndex(m => m.InventoryItemId);
        builder.HasIndex(m => m.ActivityLogId);
    }
}
