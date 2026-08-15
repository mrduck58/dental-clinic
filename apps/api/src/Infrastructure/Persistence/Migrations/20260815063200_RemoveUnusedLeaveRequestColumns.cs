using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalClinic.API.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Dọn 4 cột LeaveRequests không có ai đọc. Migration ngay trước đó
    /// (AddLeaveRequestScheduleImpact) tạo chúng, nhưng entity <c>LeaveRequest</c> không hề khai báo
    /// property tương ứng — các con số này được TÍNH LÚC CHẠY trong handler rồi trả thẳng ra DTO,
    /// không bao giờ được lưu. Vì vậy EF không hề biết tới 4 cột này, và mọi lệnh
    /// <c>migrations add</c> sau đó đều tự kèm 4 lệnh DropColumn lạc đề vào migration của người khác.
    ///
    /// Migration này viết tay: model và snapshot đã khớp nhau từ trước nên EF scaffold ra file rỗng —
    /// việc cần làm chỉ nằm ở phía cơ sở dữ liệu.
    /// </summary>
    public partial class RemoveUnusedLeaveRequestColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AffectedAppointmentCount",
                table: "LeaveRequests");

            migrationBuilder.DropColumn(
                name: "AffectedDayCount",
                table: "LeaveRequests");

            migrationBuilder.DropColumn(
                name: "RemovedShiftCount",
                table: "LeaveRequests");

            migrationBuilder.DropColumn(
                name: "ScheduleWeekStart",
                table: "LeaveRequests");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AffectedAppointmentCount",
                table: "LeaveRequests",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AffectedDayCount",
                table: "LeaveRequests",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RemovedShiftCount",
                table: "LeaveRequests",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "ScheduleWeekStart",
                table: "LeaveRequests",
                type: "date",
                nullable: true);
        }
    }
}
