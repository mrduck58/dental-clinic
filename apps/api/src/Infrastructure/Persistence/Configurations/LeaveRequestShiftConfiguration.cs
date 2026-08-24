using DentalClinic.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalClinic.API.Infrastructure.Persistence.Configurations;

public class LeaveRequestShiftConfiguration : IEntityTypeConfiguration<LeaveRequestShift>
{
    public void Configure(EntityTypeBuilder<LeaveRequestShift> builder)
    {
        builder.ToTable("LeaveRequestShifts");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.ShiftId).HasMaxLength(20).IsRequired();

        // Cùng một ca/ngày không thể xuất hiện hai lần trong một đơn nghỉ.
        builder.HasIndex(s => new { s.LeaveRequestId, s.Date, s.ShiftId }).IsUnique();
    }
}
