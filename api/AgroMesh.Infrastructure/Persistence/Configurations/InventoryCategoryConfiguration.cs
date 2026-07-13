using AgroMesh.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgroMesh.Infrastructure.Persistence.Configurations;

public class InventoryCategoryConfiguration : IEntityTypeConfiguration<InventoryCategory>
{
    public void Configure(EntityTypeBuilder<InventoryCategory> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.FarmId).IsRequired().HasMaxLength(50);
        builder.Property(c => c.Name).IsRequired().HasMaxLength(100);
        builder.Property(c => c.IsDefault).IsRequired();

        builder.HasMany(c => c.Items)
               .WithOne(i => i.Category)
               .HasForeignKey(i => i.CategoryId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => c.FarmId);
        builder.HasIndex(c => new { c.FarmId, c.Name });
    }
}
