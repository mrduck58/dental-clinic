using DentalClinic.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalClinic.API.Infrastructure.Persistence.Configurations;

public class DentistProfileConfiguration : IEntityTypeConfiguration<DentistProfile>
{
    public void Configure(EntityTypeBuilder<DentistProfile> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Specialization).IsRequired().HasMaxLength(100);
        builder.Property(d => d.LicenseNumber).IsRequired().HasMaxLength(100);
        builder.Property(d => d.Education).HasMaxLength(200);
        builder.Property(d => d.Biography).HasMaxLength(2000);
        builder.Property(d => d.CertificateIssuedBy).HasMaxLength(200);
        builder.Property(d => d.Shift).IsRequired().HasMaxLength(50);

        // Required 1-to-1 relationship with Employee (không trỏ thẳng User nữa)
        builder.HasOne(d => d.Employee)
            .WithOne(e => e.DentistProfile)
            .HasForeignKey<DentistProfile>(d => d.EmployeeId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}
