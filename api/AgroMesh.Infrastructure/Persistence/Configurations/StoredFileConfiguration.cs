using AgroMesh.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgroMesh.Infrastructure.Persistence.Configurations;

public class StoredFileConfiguration : IEntityTypeConfiguration<StoredFile>
{
    public void Configure(EntityTypeBuilder<StoredFile> builder)
    {
        builder.HasKey(f => f.Id);
        builder.Property(f => f.FarmId).IsRequired().HasMaxLength(50);
        builder.Property(f => f.FileName).IsRequired().HasMaxLength(200);
        builder.Property(f => f.ContentType).IsRequired().HasMaxLength(100);
        builder.Property(f => f.Size).IsRequired();
        builder.Property(f => f.Content).IsRequired().HasColumnType("bytea");

        builder.HasIndex(f => f.FarmId);
    }
}
