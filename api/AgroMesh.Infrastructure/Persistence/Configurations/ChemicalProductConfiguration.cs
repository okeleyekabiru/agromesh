using AgroMesh.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgroMesh.Infrastructure.Persistence.Configurations;

public class ChemicalProductConfiguration : IEntityTypeConfiguration<ChemicalProduct>
{
    public void Configure(EntityTypeBuilder<ChemicalProduct> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).IsRequired().HasMaxLength(200);
        builder.Property(c => c.Manufacturer).HasMaxLength(200);
        builder.Property(c => c.ChemType).IsRequired();
        builder.Property(c => c.TypicalIntervalDays).IsRequired();
        builder.Property(c => c.Active).IsRequired();

        builder.HasIndex(c => c.Active);
        builder.HasIndex(c => c.ChemType);
    }
}
