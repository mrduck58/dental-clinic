using DentalClinic.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalClinic.API.Infrastructure.Persistence.Configurations;

public class StaffConfiguration : IEntityTypeConfiguration<Staff>
{
    public void Configure(EntityTypeBuilder<Staff> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.EmployeeId).IsRequired().HasMaxLength(50);
        builder.Property(s => s.Department).HasMaxLength(100);
        builder.Property(s => s.Position).HasMaxLength(100);
        builder.Property(s => s.EmploymentStatus).IsRequired().HasMaxLength(50);
        builder.Property(s => s.EmploymentType).HasMaxLength(50);
        builder.Property(s => s.Address).HasMaxLength(500);
        builder.Property(s => s.ProfilePictureUrl);
        builder.Property(s => s.BaseSalary).HasPrecision(18, 2);
        builder.Property(s => s.SalaryUnit).HasMaxLength(50);
        builder.Property(s => s.Allowance).HasPrecision(18, 2);
        builder.Property(s => s.LeaveAccrued).HasPrecision(5, 2);

        // Required 1-to-1 relationship with User
        builder.HasOne(s => s.User)
            .WithOne(u => u.Staff)
            .HasForeignKey<Staff>(s => s.UserId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}
