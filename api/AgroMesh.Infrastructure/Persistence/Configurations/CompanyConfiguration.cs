using AgroMesh.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgroMesh.Infrastructure.Persistence.Configurations;

public class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).IsRequired().HasMaxLength(200);
        builder.Property(c => c.OwnerId).IsRequired().HasMaxLength(100);
        builder.Property(c => c.IsDefaultCompany).IsRequired();

        builder.HasMany(c => c.Farms)
               .WithOne()
               .HasForeignKey(f => f.CompanyId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => c.OwnerId);
    }
}
