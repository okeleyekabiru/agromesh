using AgroMesh.Domain.Entities;
using AgroMesh.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgroMesh.Infrastructure.Persistence.Configurations;

public class ActivityLogConfiguration : IEntityTypeConfiguration<ActivityLog>
{
    public void Configure(EntityTypeBuilder<ActivityLog> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.FarmId).IsRequired().HasMaxLength(50);
        builder.Property(a => a.SectionId).IsRequired().HasMaxLength(50);
        builder.Property(a => a.BlockId).IsRequired().HasMaxLength(50);
        builder.Property(a => a.ActivityType).IsRequired();
        builder.Property(a => a.Date).IsRequired();
        builder.Property(a => a.SupervisorId).IsRequired().HasMaxLength(100);
        builder.Property(a => a.LabourCost).HasColumnType("decimal(14,2)");
        builder.Property(a => a.InputCost).HasColumnType("decimal(14,2)");
        builder.Property(a => a.Notes).HasMaxLength(2000);
        builder.Property(a => a.WorkedAreaGeoJson).HasColumnType("text");
        builder.Property(a => a.WorkedHectares);
        builder.Property(a => a.ChemicalProductId).HasMaxLength(50);
        builder.Property(a => a.ChemicalProductName).HasMaxLength(200);
        builder.Property(a => a.CreatedOffline).IsRequired();
        builder.Property(a => a.SyncedAt);

        builder.Property(a => a.Workers)
               .HasColumnType("jsonb")
               .HasConversion(
                   v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                   v => System.Text.Json.JsonSerializer.Deserialize<List<WorkerLog>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new());

        builder.Property(a => a.Inputs)
               .HasColumnType("jsonb")
               .HasConversion(
                   v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                   v => System.Text.Json.JsonSerializer.Deserialize<List<InputUsed>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new());

        builder.HasIndex(a => new { a.FarmId, a.Date });
        builder.HasIndex(a => a.SectionId);
        builder.HasIndex(a => a.BlockId);
    }
}
