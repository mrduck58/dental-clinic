using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalClinic.API.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Nối các dòng lịch làm việc CŨ về đúng nhân sự qua <c>WorkSchedules.EmployeeId</c>.
    ///
    /// <para>
    /// Cột khóa ngoại này có từ đầu nhưng chưa bao giờ được ghi: <c>WorkSchedule.Create</c> không
    /// nhận nó, nên mọi dòng đều NULL và sợi dây duy nhất nối dòng lịch với bác sĩ là chuỗi
    /// <c>StaffName</c>. Hai bảng lại viết tên khác nhau — hồ sơ ghi "BS.Đỗ Văn Phong", bảng xếp lịch
    /// ghi "Đỗ Văn Phong" — nên phép so chuỗi chính xác ở tầng đọc làm bác sĩ biến mất khỏi lưới đặt
    /// lịch tại quầy: xếp ca 4 người, lễ tân chỉ chọn được 1.
    /// </para>
    ///
    /// <para>
    /// Chuẩn hoá dưới đây phải khớp với <c>StaffNameMatcher.Key</c> ở tầng Domain: bỏ chức danh đứng
    /// đầu, gom dấu cách, bỏ phân biệt hoa thường.
    /// </para>
    ///
    /// Migration viết tay vì thay đổi nằm ở DỮ LIỆU, không phải ở lược đồ — EF không sinh ra được.
    /// </summary>
    public partial class BackfillWorkScheduleEmployeeId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                WITH normalized_employees AS (
                    SELECT
                        e."Id" AS employee_id,
                        regexp_replace(
                            regexp_replace(
                                lower(btrim(u."FullName")),
                                '^(b\.?s\.?|p\.?t\.?|bác\s*sĩ|bac\s*si|dr\.?)\s*\.?\s*', ''
                            ),
                            '\s+', ' ', 'g'
                        ) AS name_key
                    FROM "Employees" e
                    JOIN "Users" u ON u."Id" = e."UserId"
                    WHERE u."FullName" IS NOT NULL AND btrim(u."FullName") <> ''
                ),
                -- Hai người cùng tên sau khi chuẩn hoá thì BỎ QUA cả hai: gán bừa một người còn tệ
                -- hơn để trống, vì ca sẽ chạy sang bác sĩ khác mà không ai nhận ra.
                unambiguous AS (
                    -- Postgres không có min(uuid); HAVING bên dưới đã bảo đảm nhóm chỉ còn đúng
                    -- một employee_id nên lấy phần tử đầu của mảng là đủ.
                    SELECT name_key, (array_agg(DISTINCT employee_id))[1] AS employee_id
                    FROM normalized_employees
                    WHERE name_key <> ''
                    GROUP BY name_key
                    HAVING count(DISTINCT employee_id) = 1
                )
                UPDATE "WorkSchedules" ws
                SET "EmployeeId" = u.employee_id
                FROM unambiguous u
                WHERE ws."EmployeeId" IS NULL
                  AND regexp_replace(
                          regexp_replace(
                              lower(btrim(ws."StaffName")),
                              '^(b\.?s\.?|p\.?t\.?|bác\s*sĩ|bac\s*si|dr\.?)\s*\.?\s*', ''
                          ),
                          '\s+', ' ', 'g'
                      ) = u.name_key;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Không gỡ được chọn lọc: sau khi chạy Up, không còn phân biệt được dòng nào vừa được
            // backfill với dòng nào đã có EmployeeId từ trước. Để nguyên là lựa chọn an toàn —
            // dữ liệu thừa ở đây vô hại, còn xoá trắng cột sẽ mất cả liên kết do người dùng tạo.
        }
    }
}
