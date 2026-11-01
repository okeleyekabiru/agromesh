using AgroMesh.Domain.Entities;
using AgroMesh.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgroMesh.Infrastructure.Persistence.Configurations;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.FarmId).IsRequired().HasMaxLength(50);
        builder.Property(i => i.Number).IsRequired();
        builder.Property(i => i.Date).IsRequired().HasColumnType("date");
        builder.Property(i => i.DueDate).IsRequired().HasColumnType("date");
        builder.Property(i => i.BillToName).IsRequired().HasMaxLength(200);
        builder.Property(i => i.BillToPhone).HasMaxLength(60);
        builder.Property(i => i.BillToEmail).HasMaxLength(200);
        builder.Property(i => i.BillToAddress).HasMaxLength(500);
        builder.Property(i => i.VatPercent).HasPrecision(5, 2);
        builder.Property(i => i.PaymentAccountName).HasMaxLength(150);
        builder.Property(i => i.PaymentAccountNumber).HasMaxLength(40);
        builder.Property(i => i.PaymentBankName).HasMaxLength(100);
        builder.Property(i => i.TermsAndNotes).HasMaxLength(2000);
        builder.Property(i => i.SignatoryName).HasMaxLength(150);
        builder.Property(i => i.TemplateKey).IsRequired().HasMaxLength(50);
        builder.Property(i => i.CreatedByUserId).HasMaxLength(100);

        builder.Ignore(i => i.Reference);
        builder.Ignore(i => i.Subtotal);
        builder.Ignore(i => i.VatAmount);
        builder.Ignore(i => i.Total);

        // Line items are stored as one jsonb column, like Order.LineItems: no query filters on an individual line.
        builder.Property(i => i.LineItems)
               .HasColumnType("jsonb")
               .HasConversion(
                   v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                   v => System.Text.Json.JsonSerializer.Deserialize<List<InvoiceLineItem>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new());

        builder.HasOne<Farm>()
               .WithMany()
               .HasForeignKey(i => i.FarmId)
               .OnDelete(DeleteBehavior.Cascade);

        // One number per farm, ever: this is what makes concurrent numbering safe (the loser retries).
        builder.HasIndex(i => new { i.FarmId, i.Number }).IsUnique();
    }
}
