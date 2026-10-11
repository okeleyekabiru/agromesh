using AgroMesh.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgroMesh.Infrastructure.Persistence.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.FarmId).IsRequired().HasMaxLength(50);
        builder.Property(t => t.MonoId).IsRequired().HasMaxLength(100);
        builder.Property(t => t.Narration).IsRequired().HasMaxLength(500);
        builder.Property(t => t.Amount).IsRequired().HasColumnType("decimal(14,2)");
        builder.Property(t => t.Date).IsRequired();
        builder.Property(t => t.Category).IsRequired();
        builder.Property(t => t.Confidence).IsRequired();
        builder.Property(t => t.Source).IsRequired();
        builder.Property(t => t.ReviewedByUser).IsRequired();
        builder.Property(t => t.ReviewNotes).HasMaxLength(500);
        builder.Property(t => t.Kind).IsRequired();
        builder.Property(t => t.OrderId).HasMaxLength(50);
        builder.Property(t => t.Reconciliation).IsRequired();
        builder.Property(t => t.ReconciliationNote).HasMaxLength(500);

        builder.Property(t => t.CropAllocation)
               .HasColumnType("jsonb")
               .HasConversion(
                   v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                   v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, double>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new());

        builder.HasIndex(t => t.MonoId).IsUnique();
        builder.HasIndex(t => new { t.FarmId, t.Date });
        builder.HasIndex(t => new { t.FarmId, t.ReviewedByUser });
        builder.HasIndex(t => new { t.FarmId, t.Reconciliation });
    }
}
