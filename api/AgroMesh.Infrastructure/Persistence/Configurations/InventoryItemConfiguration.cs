using AgroMesh.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgroMesh.Infrastructure.Persistence.Configurations;

public class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(EntityTypeBuilder<InventoryItem> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.FarmId).IsRequired().HasMaxLength(50);
        builder.Property(i => i.Name).IsRequired().HasMaxLength(200);
        builder.Property(i => i.Unit).IsRequired().HasMaxLength(50);
        builder.Property(i => i.CategoryId).IsRequired().HasMaxLength(50);
        builder.Property(i => i.CurrentStock).IsRequired().HasColumnType("decimal(14,3)");
        builder.Property(i => i.ReorderThreshold).IsRequired().HasColumnType("decimal(14,3)");
        builder.Property(i => i.ChemicalProductId).HasMaxLength(50);

        builder.Ignore(i => i.IsLowStock);

        builder.HasMany(i => i.Movements)
               .WithOne()
               .HasForeignKey(m => m.InventoryItemId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(i => new { i.FarmId, i.CategoryId });
        builder.HasIndex(i => i.ChemicalProductId);
    }
}
