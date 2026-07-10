using DentalClinic.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalClinic.API.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Username).HasMaxLength(100);   // nullable — employees may not have an account yet
        builder.Property(u => u.Email).IsRequired().HasMaxLength(255);
        builder.Property(u => u.PasswordHash);                 // nullable — same reason
        builder.Property(u => u.Role).IsRequired().HasMaxLength(50);
        builder.Property(u => u.PhoneNumber).HasMaxLength(20);

        builder.Property(u => u.EmployeeId).HasMaxLength(50);
        builder.Property(u => u.Department).HasMaxLength(100);
        builder.Property(u => u.EmploymentStatus).HasMaxLength(50);

        // Doctor-specific fields
        builder.Property(u => u.Specialty).HasMaxLength(100);
        builder.Property(u => u.LicenseNumber).HasMaxLength(100);

        // Extended staff/doctor fields
        builder.Property(u => u.Gender).HasMaxLength(20);
        builder.Property(u => u.Address).HasMaxLength(500);
        builder.Property(u => u.ServicesHandled).HasMaxLength(500);
        builder.Property(u => u.CertificateIssuedBy).HasMaxLength(200);
        builder.Property(u => u.Education).HasMaxLength(200);
        builder.Property(u => u.Bio).HasMaxLength(2000);
        builder.Property(u => u.Position).HasMaxLength(100);

        // Salary & Leave fields
        builder.Property(u => u.EmploymentType).HasMaxLength(50);
        builder.Property(u => u.BaseSalary).HasPrecision(18, 2);
        builder.Property(u => u.SalaryUnit).HasMaxLength(50);
        builder.Property(u => u.LeaveAccrued).HasPrecision(5, 2);
        builder.Property(u => u.Allowance).HasPrecision(18, 2);

        // Password reset fields
        builder.Property(u => u.PasswordResetToken).HasMaxLength(100);

        // External auth provider (null = local account)
        builder.Property(u => u.Provider).HasMaxLength(50);

        builder.HasIndex(u => u.Email).IsUnique();
        builder.HasIndex(u => u.Username).IsUnique().HasFilter("\"Username\" IS NOT NULL");
    }
}
