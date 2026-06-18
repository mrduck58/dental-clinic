using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DentalClinic.API.src.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedFeedbackData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Feedbacks",
                columns: new[] { "Id", "Comment", "CreatedAt", "CustomerName", "Rating", "RepliedAt", "ReplyText", "Status" },
                values: new object[,]
                {
                    { new Guid("a1b2c3d4-0001-0000-0000-000000000001"), "Bác sĩ rất tận tình, giải thích rõ ràng từng bước điều trị. Phòng khám sạch sẽ, trang thiết bị hiện đại. Tôi rất hài lòng!", new DateTimeOffset(new DateTime(2026, 5, 14, 8, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Nguyễn Thị Lan", 5, new DateTimeOffset(new DateTime(2026, 5, 15, 10, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Cảm ơn chị Lan đã tin tưởng phòng khám. Chúng tôi rất vui khi được phục vụ chị!", "Approved" },
                    { new Guid("a1b2c3d4-0001-0000-0000-000000000002"), "Dịch vụ tốt, nhân viên thân thiện. Chỉ hơi chờ lâu một chút nhưng nhìn chung rất ổn.", new DateTimeOffset(new DateTime(2026, 5, 20, 9, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Trần Văn Minh", 4, null, null, "Pending" },
                    { new Guid("a1b2c3d4-0001-0000-0000-000000000003"), "Lần đầu đến phòng khám, được tư vấn miễn phí rất chi tiết. Bác sĩ chuyên nghiệp, nhẹ nhàng. Sẽ quay lại!", new DateTimeOffset(new DateTime(2026, 5, 22, 14, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Phạm Thu Hương", 5, null, null, "Approved" },
                    { new Guid("a1b2c3d4-0001-0000-0000-000000000004"), "Chất lượng điều trị ổn nhưng thời gian đợi khá lâu, khoảng 30 phút so với lịch hẹn.", new DateTimeOffset(new DateTime(2026, 6, 1, 11, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Lê Hoàng Nam", 3, null, null, "Pending" },
                    { new Guid("a1b2c3d4-0001-0000-0000-000000000005"), "Phòng khám rất sạch sẽ và hiện đại. Bác sĩ giỏi, không đau chút nào khi nhổ răng. Cực kỳ hài lòng!", new DateTimeOffset(new DateTime(2026, 6, 2, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Võ Thị Mai", 5, new DateTimeOffset(new DateTime(2026, 6, 3, 9, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Cảm ơn chị Mai đã chia sẻ! Phòng khám luôn cố gắng mang lại trải nghiệm tốt nhất.", "Approved" },
                    { new Guid("a1b2c3d4-0001-0000-0000-000000000006"), "Giá hơi cao so với các phòng khám khác. Dịch vụ tạm ổn nhưng không đặc biệt.", new DateTimeOffset(new DateTime(2026, 6, 5, 10, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Đặng Quốc Hùng", 2, null, null, "Hidden" },
                    { new Guid("a1b2c3d4-0001-0000-0000-000000000007"), "Môi trường phòng khám thoáng mát, nhân viên lễ phép. Bác sĩ giải thích kỹ tình trạng răng miệng.", new DateTimeOffset(new DateTime(2026, 6, 8, 8, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Bùi Thị Thanh", 4, null, null, "Pending" },
                    { new Guid("a1b2c3d4-0001-0000-0000-000000000008"), "Đã điều trị tại đây được 2 năm. Chất lượng luôn ổn định, bác sĩ theo dõi sát sao tình trạng bệnh nhân.", new DateTimeOffset(new DateTime(2026, 6, 9, 16, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Ngô Tiến Dũng", 5, new DateTimeOffset(new DateTime(2026, 6, 10, 11, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Cảm ơn anh Dũng đã đồng hành cùng phòng khám! Chúng tôi rất trân trọng sự tin tưởng của anh.", "Approved" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Feedbacks",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3d4-0001-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "Feedbacks",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3d4-0001-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "Feedbacks",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3d4-0001-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                table: "Feedbacks",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3d4-0001-0000-0000-000000000004"));

            migrationBuilder.DeleteData(
                table: "Feedbacks",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3d4-0001-0000-0000-000000000005"));

            migrationBuilder.DeleteData(
                table: "Feedbacks",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3d4-0001-0000-0000-000000000006"));

            migrationBuilder.DeleteData(
                table: "Feedbacks",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3d4-0001-0000-0000-000000000007"));

            migrationBuilder.DeleteData(
                table: "Feedbacks",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3d4-0001-0000-0000-000000000008"));
        }
    }
}
