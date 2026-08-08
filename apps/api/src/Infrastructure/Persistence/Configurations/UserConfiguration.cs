using DentalClinic.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalClinic.API.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Username).HasMaxLength(100);   // nullable
        builder.Property(u => u.Email).HasMaxLength(255);       // nullable now
        builder.Property(u => u.PasswordHash);                 // nullable
        builder.Property(u => u.Role).IsRequired().HasConversion<string>().HasMaxLength(50);
        builder.Property(u => u.FullName).IsRequired().HasMaxLength(200);
        builder.Property(u => u.Gender).HasMaxLength(20);
        builder.Property(u => u.PhoneNumber).HasMaxLength(20);

        // Password reset fields
        builder.Property(u => u.PasswordResetToken).HasMaxLength(100);

        // External auth provider (null = local account)
        builder.Property(u => u.Provider).HasMaxLength(50);

        // Brute-force lockout. Default 0 so the column backfills cleanly on existing rows.
        builder.Property(u => u.FailedLoginAttempts).IsRequired().HasDefaultValue(0);
        builder.Property(u => u.LockoutEndAt); // null = không bị khóa

        builder.HasIndex(u => u.Email).IsUnique().HasFilter("\"Email\" IS NOT NULL");
        builder.HasIndex(u => u.Username).IsUnique().HasFilter("\"Username\" IS NOT NULL");
    }
}
