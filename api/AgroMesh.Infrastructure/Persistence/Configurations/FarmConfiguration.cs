using AgroMesh.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgroMesh.Infrastructure.Persistence.Configurations;

public class FarmConfiguration : IEntityTypeConfiguration<Farm>
{
    public void Configure(EntityTypeBuilder<Farm> builder)
    {
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Name).IsRequired().HasMaxLength(200);
        builder.Property(f => f.Location).IsRequired().HasMaxLength(300);
        builder.Property(f => f.OwnerId).IsRequired().HasMaxLength(100);
        builder.Property(f => f.TotalHectares).IsRequired();
        builder.Property(f => f.PreferredAreaUnit).IsRequired();
        builder.Property(f => f.CentroidLat);
        builder.Property(f => f.CentroidLng);

        builder.HasMany(f => f.Sections)
               .WithOne()
               .HasForeignKey(s => s.FarmId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(f => f.WorkerRoles)
               .WithOne()
               .HasForeignKey(r => r.FarmId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(f => f.Workers)
               .WithOne()
               .HasForeignKey(w => w.FarmId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(f => f.AllocationRules)
               .WithOne()
               .HasForeignKey(r => r.FarmId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(f => f.InventoryItems)
               .WithOne()
               .HasForeignKey(i => i.FarmId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(f => f.OwnerId);
    }
}
