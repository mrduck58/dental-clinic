using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalClinic.API.src.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPrescriptionItemReminderFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DurationDays",
                table: "PrescriptionItems",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "StartDate",
                table: "PrescriptionItems",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TimesPerDay",
                table: "PrescriptionItems",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DurationDays",
                table: "PrescriptionItems");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "PrescriptionItems");

            migrationBuilder.DropColumn(
                name: "TimesPerDay",
                table: "PrescriptionItems");
        }
    }
}
