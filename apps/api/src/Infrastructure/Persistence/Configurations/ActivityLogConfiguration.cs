using DentalClinic.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalClinic.API.Infrastructure.Persistence.Configurations;

public class ActivityLogConfiguration : IEntityTypeConfiguration<ActivityLog>
{
    public void Configure(EntityTypeBuilder<ActivityLog> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.UserName).IsRequired().HasMaxLength(200);
        builder.Property(a => a.UserRole).IsRequired().HasMaxLength(50);
        builder.Property(a => a.Action).IsRequired().HasMaxLength(50);
        builder.Property(a => a.Module).IsRequired().HasMaxLength(50);
        builder.Property(a => a.Description).IsRequired().HasMaxLength(1000);
        builder.Property(a => a.IpAddress).HasMaxLength(50);
        builder.Property(a => a.Status).IsRequired().HasMaxLength(20);
        builder.Property(a => a.TargetId).HasMaxLength(100);

        builder.HasIndex(a => a.CreatedAt);
        builder.HasIndex(a => a.UserId);
        builder.HasIndex(a => new { a.Module, a.Action });
    }
}
