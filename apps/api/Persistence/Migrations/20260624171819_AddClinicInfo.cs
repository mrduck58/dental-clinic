using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalClinic.API.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddClinicInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClinicInfos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AboutTitle = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    AboutDescription = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    FoundedYear = table.Column<int>(type: "integer", nullable: false),
                    AboutImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Phone = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    MilestonesJson = table.Column<string>(type: "text", nullable: false, defaultValue: "[]"),
                    CertificationsJson = table.Column<string>(type: "text", nullable: false, defaultValue: "[]"),
                    FeaturesJson = table.Column<string>(type: "text", nullable: false, defaultValue: "[]"),
                    TreatmentStepsJson = table.Column<string>(type: "text", nullable: false, defaultValue: "[]"),
                    StatisticsJson = table.Column<string>(type: "text", nullable: false, defaultValue: "[]"),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicInfos", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "ClinicInfos",
                columns: new[] { "Id", "AboutDescription", "AboutImageUrl", "AboutTitle", "Address", "CertificationsJson", "CreatedAt", "Email", "FeaturesJson", "FoundedYear", "MilestonesJson", "Phone", "StatisticsJson", "TreatmentStepsJson", "UpdatedAt" },
                values: new object[] { new Guid("c1100000-0000-0000-0000-000000000001"), "Sơn Giang Dental được thành lập năm 2009 với sứ mệnh mang lại dịch vụ chăm sóc răng miệng chất lượng cao, tiệm cận chuẩn quốc tế, với mức chi phí phù hợp nhất cho người Việt.\n\nTrải qua hơn 15 năm phát triển, chúng tôi đã xây dựng được đội ngũ hơn 20 bác sĩ chuyên khoa, trang bị công nghệ điều trị hàng đầu và phục vụ hơn 10.000 khách hàng hài lòng trên khắp cả nước.", null, "Hơn 15 Năm Kiến Tạo Nụ Cười Việt Nam", "123 Đường Ba Tháng Hai, Quận 10, TP.HCM", "[\"ISO 9001:2015\",\"Invisalign Provider\",\"Bộ Y tế cấp phép\",\"ADA Member\"]", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "contact@songiangdental.vn", "[{\"title\":\"Công nghệ hiện đại hàng đầu\",\"description\":\"Trang bị máy CT Cone Beam 3D, laser nha khoa, kính lúp phẫu thuật và hệ thống CAD/CAM làm sứ ngay tại phòng khám.\"},{\"title\":\"Đội ngũ bác sĩ chuyên sâu\",\"description\":\"100% bác sĩ có chứng chỉ chuyên khoa, tu nghiệp tại Pháp, Mỹ, Nhật — liên tục cập nhật kỹ thuật mới nhất.\"},{\"title\":\"Cam kết minh bạch giá cả\",\"description\":\"Báo giá rõ ràng trước điều trị, không phát sinh, bảo hành lên đến 10 năm cho các ca phục hình cao cấp.\"},{\"title\":\"Môi trường vô trùng tuyệt đối\",\"description\":\"Quy trình tiệt khuẩn đạt chuẩn CDC/ADA, mỗi bệnh nhân dùng bộ dụng cụ riêng đóng gói sealed.\"}]", 2009, "[{\"year\":2009,\"description\":\"Thành lập phòng khám đầu tiên tại TP.HCM\"},{\"year\":2015,\"description\":\"Mở rộng lên 3 cơ sở, đạt chứng nhận ISO 9001\"},{\"year\":2019,\"description\":\"Đối tác chính thức của Invisalign tại Việt Nam\"},{\"year\":2023,\"description\":\"Ra mắt ứng dụng đặt lịch trên di động\"}]", "1900 6789 — 028 7300 1234", "[{\"value\":\"10.000+\",\"label\":\"Khách hàng hài lòng\"},{\"value\":\"20+\",\"label\":\"Bác sĩ chuyên khoa\"},{\"value\":\"15+\",\"label\":\"Năm kinh nghiệm\"},{\"value\":\"99%\",\"label\":\"Đánh giá 5 sao\"}]", "[{\"title\":\"Tải App & Đặt Lịch\",\"description\":\"Tải app Sơn Giang Dental, chọn dịch vụ và đặt lịch trong 30 giây. Xác nhận ngay lập tức.\"},{\"title\":\"Khám & Tư Vấn\",\"description\":\"Bác sĩ chuyên khoa thăm khám toàn diện, chụp X-quang và tư vấn phác đồ phù hợp.\"},{\"title\":\"Điều Trị\",\"description\":\"Thực hiện điều trị theo phác đồ đã tư vấn với công nghệ hiện đại, không đau, an toàn.\"},{\"title\":\"Theo Dõi Sau Điều Trị\",\"description\":\"Tái khám định kỳ miễn phí, bảo hành dài hạn và hỗ trợ 24/7 khi có vấn đề phát sinh.\"}]", null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClinicInfos");
        }
    }
}
