using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalClinic.API.src.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameEveningShiftCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Đổi 2 ca tối từ 1h45 mỗi ca (17:30-19:15 / 19:15-21:00) sang 2 tiếng mỗi ca
            // (17:30-19:30 / 19:30-21:30), khớp với các ca sáng/chiều còn lại — xem
            // DentalClinic.API.Domain.Schedules.WorkShifts. Cập nhật dữ liệu WorkSchedule
            // đã lưu để không bị "mồ côi" (orphan) theo mã ca cũ.
            migrationBuilder.Sql(
                "UPDATE \"WorkSchedules\" SET \"Shift\" = '17:30-19:30' WHERE \"Shift\" = '17:30-19:15';");
            migrationBuilder.Sql(
                "UPDATE \"WorkSchedules\" SET \"Shift\" = '19:30-21:30' WHERE \"Shift\" = '19:15-21:00';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE \"WorkSchedules\" SET \"Shift\" = '17:30-19:15' WHERE \"Shift\" = '17:30-19:30';");
            migrationBuilder.Sql(
                "UPDATE \"WorkSchedules\" SET \"Shift\" = '19:15-21:00' WHERE \"Shift\" = '19:30-21:30';");
        }
    }
}
