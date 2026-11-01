using AgroMesh.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgroMesh.Infrastructure.Persistence.Configurations;

public class VendorInvoiceConfiguration : IEntityTypeConfiguration<VendorInvoice>
{
    public void Configure(EntityTypeBuilder<VendorInvoice> builder)
    {
        builder.HasKey(v => v.Id);
        builder.Property(v => v.FarmId).IsRequired().HasMaxLength(50);
        builder.Property(v => v.OrderId).IsRequired().HasMaxLength(50);
        builder.Property(v => v.SupplierId).IsRequired().HasMaxLength(50);
        builder.Property(v => v.FileId).IsRequired().HasMaxLength(50);
        builder.Property(v => v.FileName).IsRequired().HasMaxLength(200);
        builder.Property(v => v.ContentType).IsRequired().HasMaxLength(100);
        builder.Property(v => v.VendorName).IsRequired().HasMaxLength(200);
        builder.Property(v => v.InvoiceNumber).IsRequired().HasMaxLength(100);
        builder.Property(v => v.InvoiceNumberKey).IsRequired().HasMaxLength(VendorInvoice.MaxNumberKeyLength);
        builder.Property(v => v.Amount).HasPrecision(18, 2);
        builder.Property(v => v.InvoiceDate).IsRequired().HasColumnType("date");
        builder.Property(v => v.DueDate).HasColumnType("date");
        builder.Property(v => v.Status).IsRequired();
        builder.Property(v => v.Notes).HasMaxLength(1000);
        builder.Property(v => v.UploadedByUserId).HasMaxLength(100);

        builder.HasOne<Farm>()
               .WithMany()
               .HasForeignKey(v => v.FarmId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Order>()
               .WithMany()
               .HasForeignKey(v => v.OrderId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Supplier>()
               .WithMany()
               .HasForeignKey(v => v.SupplierId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(v => v.OrderId);
        builder.HasIndex(v => new { v.FarmId, v.Status });
        // One bill per vendor number per farm, case-insensitive (the key is upper-cased). Enforced by the database so two
        // simultaneous uploads of the same number cannot both succeed. SupplierId is never null (it is the order's supplier).
        builder.HasIndex(v => new { v.FarmId, v.SupplierId, v.InvoiceNumberKey }).IsUnique();
    }
}
