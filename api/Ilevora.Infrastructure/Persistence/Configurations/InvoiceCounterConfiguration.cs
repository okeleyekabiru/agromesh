using Ilevora.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ilevora.Infrastructure.Persistence.Configurations;

public class InvoiceCounterConfiguration : IEntityTypeConfiguration<InvoiceCounter>
{
    public void Configure(EntityTypeBuilder<InvoiceCounter> builder)
    {
        builder.HasKey(c => c.FarmId);
        builder.Property(c => c.FarmId).HasMaxLength(50);
        builder.Property(c => c.LastNumber).IsRequired();

        builder.HasOne<Farm>()
               .WithMany()
               .HasForeignKey(c => c.FarmId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
