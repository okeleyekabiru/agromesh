using Ilevora.Domain.Crops;
using Ilevora.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ilevora.Infrastructure.Persistence.Configurations;

public class CropConfiguration : IEntityTypeConfiguration<Crop>
{
    // Fixed timestamp so the seed rows never change between model builds.
    private static readonly DateTime SeededAt = new(2026, 11, 9, 0, 0, 0, DateTimeKind.Utc);

    public void Configure(EntityTypeBuilder<Crop> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.FarmId).HasMaxLength(50);
        builder.Property(c => c.Name).IsRequired().HasMaxLength(100);
        builder.Property(c => c.IsBuiltIn).IsRequired();
        builder.HasIndex(c => c.FarmId);

        // Built-ins: FarmId null, Id = the old CropType member name (Mixed is legacy and not seeded).
        builder.HasData(BuiltInCrop.All.Select(b => new
        {
            Id = b.Id,
            Name = b.Name,
            IsBuiltIn = true,
            CreatedAt = SeededAt,
            UpdatedAt = SeededAt,
        }));
    }
}
