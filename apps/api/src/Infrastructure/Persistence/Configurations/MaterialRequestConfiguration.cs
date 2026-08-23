using DentalClinic.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalClinic.API.Infrastructure.Persistence.Configurations;

public class MaterialRequestConfiguration : IEntityTypeConfiguration<MaterialRequest>
{
    public void Configure(EntityTypeBuilder<MaterialRequest> builder)
    {
        builder.ToTable("MaterialRequests");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.CourseName).HasMaxLength(300);
        builder.Property(m => m.PatientName).HasMaxLength(200);
        builder.Property(m => m.DentistName).HasMaxLength(200);
        builder.Property(m => m.OrderedBy).HasMaxLength(200);
        builder.Property(m => m.SupplierNote).HasMaxLength(500);
        builder.Property(m => m.HandledBy).HasMaxLength(200);

        builder.Property(m => m.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasIndex(m => m.Status);
        builder.HasIndex(m => m.TreatmentPlanId);
        builder.HasIndex(m => m.AppointmentId);

        // Restrict (không Cascade): xóa liệu trình phải tự dọn Pending liên quan trước ở tầng application
        // (DeleteTreatmentPlanHandler) — nếu còn yêu cầu Ordered/Done (đã đặt hàng/đã nhập kho thật) thì phải
        // chặn xóa hẳn, không được để DB tự xóa mất theo kiểu cascade.
        builder.HasOne<TreatmentPlan>()
            .WithMany()
            .HasForeignKey(m => m.TreatmentPlanId)
            .OnDelete(DeleteBehavior.Restrict);

        // SetNull: xóa buổi hẹn không nên kéo xóa lịch sử yêu cầu vật tư đã đặt/đã nhập kho.
        builder.HasOne<Appointment>()
            .WithMany()
            .HasForeignKey(m => m.AppointmentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(m => m.Items)
            .WithOne()
            .HasForeignKey(i => i.MaterialRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(MaterialRequest.Items))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
