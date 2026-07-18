using DentalClinic.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalClinic.API.Infrastructure.Persistence.Configurations;

public class DentistConfiguration : IEntityTypeConfiguration<Dentist>
{
    public void Configure(EntityTypeBuilder<Dentist> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.EmployeeId).IsRequired().HasMaxLength(50);
        builder.Property(d => d.Department).HasMaxLength(100);
        builder.Property(d => d.Position).HasMaxLength(100);
        builder.Property(d => d.EmploymentStatus).IsRequired().HasMaxLength(50);
        builder.Property(d => d.EmploymentType).HasMaxLength(50);
        builder.Property(d => d.Address).HasMaxLength(500);
        builder.Property(d => d.ProfilePictureUrl);
        builder.Property(d => d.BaseSalary).HasPrecision(18, 2);
        builder.Property(d => d.SalaryUnit).HasMaxLength(50);
        builder.Property(d => d.Allowance).HasPrecision(18, 2);
        builder.Property(d => d.LeaveAccrued).HasPrecision(5, 2);

        builder.Property(d => d.Specialization).IsRequired().HasMaxLength(100);
        builder.Property(d => d.LicenseNumber).IsRequired().HasMaxLength(100);
        builder.Property(d => d.Education).HasMaxLength(200);
        builder.Property(d => d.Biography).HasMaxLength(2000);
        builder.Property(d => d.CertificateIssuedBy).HasMaxLength(200);
        builder.Property(d => d.Shift).IsRequired().HasMaxLength(50);

        // Required 1-to-1 relationship with User
        builder.HasOne(d => d.User)
            .WithOne(u => u.Dentist)
            .HasForeignKey<Dentist>(d => d.UserId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}
