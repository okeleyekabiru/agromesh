using Ilevora.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ilevora.Infrastructure.Persistence.Configurations;

public class InvitationConfiguration : IEntityTypeConfiguration<Invitation>
{
    public void Configure(EntityTypeBuilder<Invitation> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.CompanyId).IsRequired().HasMaxLength(50);
        builder.Property(i => i.Email).IsRequired().HasMaxLength(320);
        builder.Property(i => i.Role).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(i => i.TokenHash).IsRequired().HasMaxLength(64);
        builder.Property(i => i.InvitedByUserId).IsRequired().HasMaxLength(50);
        builder.HasIndex(i => i.TokenHash).IsUnique();
        builder.HasIndex(i => new { i.CompanyId, i.Email });
    }
}

public class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
{
    public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.UserId).IsRequired().HasMaxLength(50);
        builder.Property(t => t.TokenHash).IsRequired().HasMaxLength(64);
        builder.HasIndex(t => t.TokenHash).IsUnique();
        builder.HasIndex(t => t.UserId);
    }
}
