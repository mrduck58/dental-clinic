using DentalClinic.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalClinic.API.Infrastructure.Persistence.Configurations;

public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.HasOne(a => a.Service)
            .WithMany()
            .HasForeignKey(a => a.ServiceId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(a => a.FollowUpFromAppointment)
            .WithMany(a => a.FollowUpAppointments)
            .HasForeignKey(a => a.FollowUpFromAppointmentId)
            .OnDelete(DeleteBehavior.SetNull);

        // Lưu lý do hủy dạng chuỗi (giống UserRole) thay vì số: báo cáo đọc thẳng bảng vẫn hiểu được,
        // và chèn thêm giá trị vào giữa enum sau này không làm sai lệch dữ liệu cũ.
        builder.Property(a => a.CancellationReason).HasConversion<string>().HasMaxLength(50);
        builder.Property(a => a.CancellationNote).HasMaxLength(500);

        builder.Property(a => a.RescheduledCount).IsRequired().HasDefaultValue(0);

        // Lọc/đếm lịch đã hủy theo nhóm lý do là truy vấn báo cáo chính của mấy cột này.
        builder.HasIndex(a => a.CancellationReason);
    }
}
