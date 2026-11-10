using Ilevora.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ilevora.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);
        builder.Property(u => u.FullName).IsRequired().HasMaxLength(200);
        builder.Property(u => u.Email).IsRequired().HasMaxLength(320);
        builder.HasIndex(u => u.Email).IsUnique();
        builder.Property(u => u.PasswordHash).IsRequired();
        builder.Property(u => u.CompanyId).HasMaxLength(50);

        // Stored as a string ("Admin"/"Member") rather than an int ordinal so a manual SELECT
        // against the DB is self-describing and a future added role doesn't silently reorder
        // meaning behind existing stored data.
        builder.Property(u => u.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(u => u.EmailNotificationsEnabled).IsRequired().HasDefaultValue(true);
        builder.Property(u => u.TokenVersion).IsRequired().HasDefaultValue(0);
        builder.Property(u => u.Role).IsRequired().HasConversion<string>().HasMaxLength(20);
    }
}
