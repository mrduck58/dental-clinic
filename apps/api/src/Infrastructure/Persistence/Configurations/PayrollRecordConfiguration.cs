using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalClinic.API.Infrastructure.Persistence.Configurations;

public class PayrollRecordConfiguration : IEntityTypeConfiguration<PayrollRecord>
{
    public void Configure(EntityTypeBuilder<PayrollRecord> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.BaseSalary).HasPrecision(18, 2);
        builder.Property(p => p.Allowance).HasPrecision(18, 2);
        builder.Property(p => p.Deduction).HasPrecision(18, 2);
        builder.Property(p => p.Bonus).HasPrecision(18, 2);
        builder.Property(p => p.NetSalary).HasPrecision(18, 2);
        builder.Property(p => p.AllowedLeaveDays).HasPrecision(5, 2);
        builder.Property(p => p.ExceededDays).HasPrecision(5, 2);
        builder.Property(p => p.Note).HasMaxLength(500);

        builder.Property(p => p.Status)
            .IsRequired()
            .HasConversion(
                v => v.ToString(),
                v => Enum.Parse<PayrollStatus>(v))
            .HasMaxLength(20);

        // Mỗi nhân sự chỉ có duy nhất một bảng lương trong một kỳ
        builder.HasIndex(p => new { p.UserId, p.Year, p.Month }).IsUnique();
        builder.HasIndex(p => new { p.Year, p.Month });

        builder.HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
