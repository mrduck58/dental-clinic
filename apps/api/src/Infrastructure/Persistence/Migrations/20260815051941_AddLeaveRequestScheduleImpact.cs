using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalClinic.API.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLeaveRequestScheduleImpact : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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
    }
}
