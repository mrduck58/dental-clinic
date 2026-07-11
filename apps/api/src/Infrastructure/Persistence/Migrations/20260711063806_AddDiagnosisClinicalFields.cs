using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalClinic.API.src.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDiagnosisClinicalFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AllergyHistory",
                table: "Diagnoses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BloodPressureDiastolic",
                table: "Diagnoses",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BloodPressureSystolic",
                table: "Diagnoses",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Conclusion",
                table: "Diagnoses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DentalCondition",
                table: "Diagnoses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "HeartRate",
                table: "Diagnoses",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MedicalHistory",
                table: "Diagnoses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Temperature",
                table: "Diagnoses",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "Diagnoses",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllergyHistory",
                table: "Diagnoses");

            migrationBuilder.DropColumn(
                name: "BloodPressureDiastolic",
                table: "Diagnoses");

            migrationBuilder.DropColumn(
                name: "BloodPressureSystolic",
                table: "Diagnoses");

            migrationBuilder.DropColumn(
                name: "Conclusion",
                table: "Diagnoses");

            migrationBuilder.DropColumn(
                name: "DentalCondition",
                table: "Diagnoses");

            migrationBuilder.DropColumn(
                name: "HeartRate",
                table: "Diagnoses");

            migrationBuilder.DropColumn(
                name: "MedicalHistory",
                table: "Diagnoses");

            migrationBuilder.DropColumn(
                name: "Temperature",
                table: "Diagnoses");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Diagnoses");
        }
    }
}
