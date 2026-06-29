using AgroMesh.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgroMesh.Infrastructure.Persistence.Configurations;

public class WorkerConfiguration : IEntityTypeConfiguration<Worker>
{
    public void Configure(EntityTypeBuilder<Worker> builder)
    {
        builder.HasKey(w => w.Id);
        builder.Property(w => w.FarmId).IsRequired().HasMaxLength(50);
        builder.Property(w => w.Name).IsRequired().HasMaxLength(200);
        builder.Property(w => w.Initials).IsRequired().HasMaxLength(10);
        builder.Property(w => w.WorkerRoleId).IsRequired().HasMaxLength(50);
        builder.Property(w => w.Type).IsRequired();
        builder.Property(w => w.Active).IsRequired();

        builder.HasIndex(w => new { w.FarmId, w.Active });
        builder.HasIndex(w => w.WorkerRoleId);
    }
}
