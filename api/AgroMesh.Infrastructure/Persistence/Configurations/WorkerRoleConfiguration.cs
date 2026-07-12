using AgroMesh.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgroMesh.Infrastructure.Persistence.Configurations;

public class WorkerRoleConfiguration : IEntityTypeConfiguration<WorkerRole>
{
    public void Configure(EntityTypeBuilder<WorkerRole> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.FarmId).IsRequired().HasMaxLength(50);
        builder.Property(r => r.Title).IsRequired().HasMaxLength(100);
        builder.Property(r => r.DefaultType).IsRequired();
        builder.Property(r => r.PayRate).IsRequired().HasColumnType("decimal(12,2)");
        builder.Property(r => r.PayFrequency).IsRequired();

        builder.HasMany(r => r.Workers)
               .WithOne(w => w.Role)
               .HasForeignKey(w => w.WorkerRoleId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => r.FarmId);
    }
}
