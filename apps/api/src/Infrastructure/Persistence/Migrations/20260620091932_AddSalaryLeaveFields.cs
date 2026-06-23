using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalClinic.API.src.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSalaryLeaveFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "BaseSalary",
                table: "Users",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmploymentType",
                table: "Users",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LeaveAccrued",
                table: "Users",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SalaryUnit",
                table: "Users",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BaseSalary",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EmploymentType",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LeaveAccrued",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SalaryUnit",
                table: "Users");
        }
    }
}
