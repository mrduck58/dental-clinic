using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalClinic.API.src.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLegacyServiceInsuranceColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // IF EXISTS: migration InitialCreate (chạy ngay trước migration này) chưa từng tạo 2 cột này —
            // migration này chỉ còn ý nghĩa lịch sử với DB cũ đã có cột từ trước khi migration được theo
            // dõi. Dùng DropColumn thẳng sẽ lỗi "column does not exist" trên bất kỳ DB nào build từ đầu
            // theo đúng chain migration hiện tại (kể cả DB trống hoàn toàn).
            migrationBuilder.Sql(@"ALTER TABLE ""Services"" DROP COLUMN IF EXISTS ""InsuranceNote"";");
            migrationBuilder.Sql(@"ALTER TABLE ""Services"" DROP COLUMN IF EXISTS ""Unit"";");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER TABLE ""Services"" ADD COLUMN IF NOT EXISTS ""InsuranceNote"" character varying(50);");
            migrationBuilder.Sql(@"ALTER TABLE ""Services"" ADD COLUMN IF NOT EXISTS ""Unit"" character varying(50);");
        }
    }
}
