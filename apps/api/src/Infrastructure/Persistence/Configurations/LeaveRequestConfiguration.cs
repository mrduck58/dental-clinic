using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalClinic.API.Infrastructure.Persistence.Configurations;

public class LeaveRequestConfiguration : IEntityTypeConfiguration<LeaveRequest>
{
    public void Configure(EntityTypeBuilder<LeaveRequest> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Reason)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(l => l.ReviewerNote)
            .HasMaxLength(500);

        builder.Property(l => l.LeaveType)
            .IsRequired()
            .HasConversion(
                v => v.ToString(),
                v => Enum.Parse<LeaveType>(v))
            .HasMaxLength(20);

        builder.Property(l => l.Status)
            .IsRequired()
            .HasConversion(
                v => v.ToString(),
                v => Enum.Parse<LeaveStatus>(v))
            .HasMaxLength(20);

        builder.HasOne(l => l.User)
            .WithMany()
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(l => l.Shifts)
            .WithOne()
            .HasForeignKey(s => s.LeaveRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(LeaveRequest.Shifts))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
