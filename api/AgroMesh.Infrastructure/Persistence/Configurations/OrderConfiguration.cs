using AgroMesh.Domain.Entities;
using AgroMesh.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgroMesh.Infrastructure.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(o => o.Id);
        builder.Property(o => o.FarmId).IsRequired().HasMaxLength(50);
        builder.Property(o => o.SupplierId).IsRequired().HasMaxLength(50);
        builder.Property(o => o.Status).IsRequired();

        // LineItems stored as a single jsonb column — same mapping shape as Section.Crops (see
        // SectionConfiguration), since no query ever needs to filter/join on an individual line item.
        builder.Property(o => o.LineItems)
               .HasColumnType("jsonb")
               .HasConversion(
                   v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                   v => System.Text.Json.JsonSerializer.Deserialize<List<OrderLineItem>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new());

        builder.HasOne<Supplier>()
               .WithMany()
               .HasForeignKey(o => o.SupplierId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(o => o.FarmId);
        builder.HasIndex(o => new { o.FarmId, o.Status });
    }
}
