using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalClinic.API.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RestoreRoomType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Cột "Type" bị một migration ngoài repo này ("RemoveRoomType", đã áp dụng thẳng lên
            // database dùng chung nhưng không có file migration tương ứng trong repo) xoá mất, trong
            // khi Room.cs, RoomConfiguration.cs và form "Tạo phòng" ở frontend vẫn bắt buộc trường
            // này — gây lỗi 42703 "column r.Type does not exist" khi tải danh sách phòng. Thêm lại
            // cột, backfill các phòng đã mất dữ liệu bằng loại mặc định của form tạo phòng.
            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "Rooms",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "Khám tổng quát");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Type",
                table: "Rooms");
        }
    }
}
