using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalClinic.API.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PayrollFormulaAndCommissionRemoval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CommissionRules");

            migrationBuilder.AddColumn<int>(
                name: "RequiredShifts",
                table: "PayrollRecords",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "RatePerShift",
                table: "Employees",
                type: "numeric",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RequiredShifts",
                table: "PayrollRecords");

            migrationBuilder.DropColumn(
                name: "RatePerShift",
                table: "Employees");

            migrationBuilder.CreateTable(
                name: "CommissionRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DentistId = table.Column<Guid>(type: "uuid", nullable: true),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RatePercent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommissionRules", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CommissionRules_DentistId",
                table: "CommissionRules",
                column: "DentistId");

            migrationBuilder.CreateIndex(
                name: "IX_CommissionRules_IsActive",
                table: "CommissionRules",
                column: "IsActive");
        }
    }
}
