using DentalClinic.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalClinic.API.Infrastructure.Persistence.Configurations;

public class ClinicInfoConfiguration : IEntityTypeConfiguration<ClinicInfo>
{
    public void Configure(EntityTypeBuilder<ClinicInfo> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.AboutTitle).IsRequired().HasMaxLength(300);
        builder.Property(c => c.AboutDescription).IsRequired().HasMaxLength(4000);
        builder.Property(c => c.AboutImageUrl).HasMaxLength(500);
        builder.Property(c => c.Phone).IsRequired().HasMaxLength(100);
        builder.Property(c => c.Email).IsRequired().HasMaxLength(255);
        builder.Property(c => c.Address).IsRequired().HasMaxLength(500);
        builder.Property(c => c.WorkingHours).IsRequired().HasMaxLength(300).HasDefaultValue("");

        // Các danh sách lưu nguyên văn dưới dạng JSON (cột text). Mặc định "[]".
        builder.Property(c => c.MilestonesJson).IsRequired().HasDefaultValue("[]");
        builder.Property(c => c.CertificationsJson).IsRequired().HasDefaultValue("[]");
        builder.Property(c => c.FeaturesJson).IsRequired().HasDefaultValue("[]");
        builder.Property(c => c.TreatmentStepsJson).IsRequired().HasDefaultValue("[]");
        builder.Property(c => c.StatisticsJson).IsRequired().HasDefaultValue("[]");

        // Seed 1 dòng singleton — nội dung khớp với data tĩnh trước đây ở clinic_website.
        builder.HasData(new
        {
            Id = Guid.Parse("c1100000-0000-0000-0000-000000000001"),
            AboutTitle = "Hơn 15 Năm Kiến Tạo Nụ Cười Việt Nam",
            AboutDescription =
                "Sơn Giang Dental được thành lập năm 2009 với sứ mệnh mang lại dịch vụ chăm sóc răng miệng chất lượng cao, tiệm cận chuẩn quốc tế, với mức chi phí phù hợp nhất cho người Việt.\n\n"
                + "Trải qua hơn 15 năm phát triển, chúng tôi đã xây dựng được đội ngũ hơn 20 bác sĩ chuyên khoa, trang bị công nghệ điều trị hàng đầu và phục vụ hơn 10.000 khách hàng hài lòng trên khắp cả nước.",
            FoundedYear = 2009,
            AboutImageUrl = (string?)null,
            Phone = "1900 6789 — 028 7300 1234",
            Email = "contact@songiangdental.vn",
            Address = "123 Đường Ba Tháng Hai, Quận 10, TP.HCM",
            WorkingHours = "T2–T6: 8:00 – 20:00 • T7–CN: 8:00 – 17:00",
            MilestonesJson =
                """[{"year":2009,"description":"Thành lập phòng khám đầu tiên tại TP.HCM"},{"year":2015,"description":"Mở rộng lên 3 cơ sở, đạt chứng nhận ISO 9001"},{"year":2019,"description":"Đối tác chính thức của Invisalign tại Việt Nam"},{"year":2023,"description":"Ra mắt ứng dụng đặt lịch trên di động"}]""",
            CertificationsJson =
                """["ISO 9001:2015","Invisalign Provider","Bộ Y tế cấp phép","ADA Member"]""",
            FeaturesJson =
                """[{"title":"Công nghệ hiện đại hàng đầu","description":"Trang bị máy CT Cone Beam 3D, laser nha khoa, kính lúp phẫu thuật và hệ thống CAD/CAM làm sứ ngay tại phòng khám."},{"title":"Đội ngũ bác sĩ chuyên sâu","description":"100% bác sĩ có chứng chỉ chuyên khoa, tu nghiệp tại Pháp, Mỹ, Nhật — liên tục cập nhật kỹ thuật mới nhất."},{"title":"Cam kết minh bạch giá cả","description":"Báo giá rõ ràng trước điều trị, không phát sinh, bảo hành lên đến 10 năm cho các ca phục hình cao cấp."},{"title":"Môi trường vô trùng tuyệt đối","description":"Quy trình tiệt khuẩn đạt chuẩn CDC/ADA, mỗi bệnh nhân dùng bộ dụng cụ riêng đóng gói sealed."}]""",
            TreatmentStepsJson =
                """[{"title":"Tải App & Đặt Lịch","description":"Tải app Sơn Giang Dental, chọn dịch vụ và đặt lịch trong 30 giây. Xác nhận ngay lập tức."},{"title":"Khám & Tư Vấn","description":"Bác sĩ chuyên khoa thăm khám toàn diện, chụp X-quang và tư vấn phác đồ phù hợp."},{"title":"Điều Trị","description":"Thực hiện điều trị theo phác đồ đã tư vấn với công nghệ hiện đại, không đau, an toàn."},{"title":"Theo Dõi Sau Điều Trị","description":"Tái khám định kỳ miễn phí, bảo hành dài hạn và hỗ trợ 24/7 khi có vấn đề phát sinh."}]""",
            StatisticsJson =
                """[{"value":"10.000+","label":"Khách hàng hài lòng"},{"value":"20+","label":"Bác sĩ chuyên khoa"},{"value":"15+","label":"Năm kinh nghiệm"},{"value":"99%","label":"Đánh giá 5 sao"}]""",
            CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            UpdatedAt = (DateTimeOffset?)null,
        });
    }
}
