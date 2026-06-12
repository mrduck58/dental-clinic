using DentalClinic.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalClinic.API.Infrastructure.Persistence.Configurations;

public class WorkScheduleConfiguration : IEntityTypeConfiguration<WorkSchedule>
{
    public void Configure(EntityTypeBuilder<WorkSchedule> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Shift).IsRequired().HasMaxLength(20);
        builder.Property(s => s.Type).IsRequired().HasMaxLength(20);
        builder.Property(s => s.Role).IsRequired().HasMaxLength(20);
        builder.Property(s => s.StaffName).IsRequired().HasMaxLength(200);
        builder.Property(s => s.Room).HasMaxLength(100);
        builder.Property(s => s.RoomColor).HasMaxLength(100);

        builder.HasIndex(s => s.Date);
    }
}
