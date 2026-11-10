using Ilevora.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ilevora.Infrastructure.Persistence.Configurations;

public class EmailOutboxConfiguration : IEntityTypeConfiguration<EmailOutboxMessage>
{
    public void Configure(EntityTypeBuilder<EmailOutboxMessage> builder)
    {
        builder.ToTable("EmailOutbox");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.To).IsRequired().HasMaxLength(320);
        builder.Property(m => m.Subject).IsRequired().HasMaxLength(300);
        builder.Property(m => m.Status).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(m => m.ClaimToken).HasMaxLength(50);
        builder.Property(m => m.LastError).HasMaxLength(200);
        builder.HasIndex(m => new { m.Status, m.NextAttemptAt });
        builder.HasIndex(m => m.ClaimToken);
    }
}
