using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalClinic.API.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAppointmentOrigin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Origin",
                table: "Appointments",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Online");

            // Dữ liệu cũ không ghi nguồn, nhưng lịch lập tại quầy để lại một dấu vết không thể nhầm:
            // nó được TẠO và CHECK-IN trong cùng một lệnh, nên hai mốc cách nhau chưa tới một phút.
            // Lịch bệnh nhân tự đặt luôn phải qua bước lễ tân xác nhận nên khoảng cách tính bằng giờ
            // hoặc ngày. Không backfill thì mọi lịch vãng lai cũ bị coi là đặt online, và hoàn tác
            // check-in sẽ đẩy chúng về hàng "chờ xác nhận" — một trạng thái chúng chưa từng có.
            migrationBuilder.Sql("""
                UPDATE "Appointments"
                SET "Origin" = 'WalkIn'
                WHERE "CheckedInAt" IS NOT NULL
                  AND ABS(EXTRACT(EPOCH FROM ("CheckedInAt" - "CreatedAt"))) < 60;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Origin",
                table: "Appointments");
        }
    }
}
