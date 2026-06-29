using AgroMesh.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgroMesh.Infrastructure.Persistence.Configurations;

public class BlockConfiguration : IEntityTypeConfiguration<Block>
{
    public void Configure(EntityTypeBuilder<Block> builder)
    {
        builder.HasKey(b => b.Id);
        builder.Property(b => b.SectionId).IsRequired().HasMaxLength(50);
        builder.Property(b => b.FarmId).IsRequired().HasMaxLength(50);
        builder.Property(b => b.Name).IsRequired().HasMaxLength(200);
        builder.Property(b => b.Hectares).IsRequired();
        builder.Property(b => b.TreeCount);

        builder.HasIndex(b => b.SectionId);
        builder.HasIndex(b => b.FarmId);
    }
}
