using DentalClinic.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalClinic.API.Infrastructure.Persistence.Configurations;

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.EmployeeId).IsRequired().HasMaxLength(50);
        builder.Property(e => e.Department).HasMaxLength(100);
        builder.Property(e => e.Position).HasMaxLength(100);
        builder.Property(e => e.EmploymentStatus).IsRequired().HasMaxLength(50);
        builder.Property(e => e.EmploymentType).HasMaxLength(50);
        builder.Property(e => e.Address).HasMaxLength(500);
        builder.Property(e => e.ProfilePictureUrl);
        builder.Property(e => e.BaseSalary).HasPrecision(18, 2);
        builder.Property(e => e.SalaryUnit).HasMaxLength(50);
        builder.Property(e => e.Allowance).HasPrecision(18, 2);
        builder.Property(e => e.LeaveAccrued).HasPrecision(5, 2);

        // Required 1-to-1 relationship with User
        builder.HasOne(e => e.User)
            .WithOne(u => u.Employee)
            .HasForeignKey<Employee>(e => e.UserId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}
