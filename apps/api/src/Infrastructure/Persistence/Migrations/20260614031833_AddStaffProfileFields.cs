using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalClinic.API.src.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStaffProfileFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // IF NOT EXISTS để tránh lỗi khi cột đã tồn tại (auto-migrate khi server restart)
            migrationBuilder.Sql("""
                ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "Department"        character varying(100);
                ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "EmployeeId"        character varying(50);
                ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "EmploymentStatus"  character varying(50);
                ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "ProfessionalNotes" text;
                ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "ProfilePictureUrl" text;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Department",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EmployeeId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EmploymentStatus",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ProfessionalNotes",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ProfilePictureUrl",
                table: "Users");
        }
    }
}
