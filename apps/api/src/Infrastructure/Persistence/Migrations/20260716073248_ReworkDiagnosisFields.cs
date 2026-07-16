using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalClinic.API.src.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReworkDiagnosisFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BloodPressureDiastolic",
                table: "Diagnoses");

            migrationBuilder.DropColumn(
                name: "BloodPressureSystolic",
                table: "Diagnoses");

            migrationBuilder.DropColumn(
                name: "DentalCondition",
                table: "Diagnoses");

            migrationBuilder.DropColumn(
                name: "DiagnosisCode",
                table: "Diagnoses");

            migrationBuilder.DropColumn(
                name: "HeartRate",
                table: "Diagnoses");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Diagnoses");

            migrationBuilder.DropColumn(
                name: "Temperature",
                table: "Diagnoses");

            migrationBuilder.AlterColumn<string>(
                name: "MedicalHistory",
                table: "Diagnoses",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Conclusion",
                table: "Diagnoses",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AllergyHistory",
                table: "Diagnoses",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BadBreath",
                table: "Diagnoses",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DecayedTeeth",
                table: "Diagnoses",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GumBleeding",
                table: "Diagnoses",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GumCondition",
                table: "Diagnoses",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LooseTeeth",
                table: "Diagnoses",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Occlusion",
                table: "Diagnoses",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OcclusionDeviation",
                table: "Diagnoses",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OralMucosaCondition",
                table: "Diagnoses",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PainOnChewing",
                table: "Diagnoses",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Plaque",
                table: "Diagnoses",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Tartar",
                table: "Diagnoses",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TeethCount",
                table: "Diagnoses",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TmjSymptoms",
                table: "Diagnoses",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WornOrBrokenTeeth",
                table: "Diagnoses",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BadBreath",
                table: "Diagnoses");

            migrationBuilder.DropColumn(
                name: "DecayedTeeth",
                table: "Diagnoses");

            migrationBuilder.DropColumn(
                name: "GumBleeding",
                table: "Diagnoses");

            migrationBuilder.DropColumn(
                name: "GumCondition",
                table: "Diagnoses");

            migrationBuilder.DropColumn(
                name: "LooseTeeth",
                table: "Diagnoses");

            migrationBuilder.DropColumn(
                name: "Occlusion",
                table: "Diagnoses");

            migrationBuilder.DropColumn(
                name: "OcclusionDeviation",
                table: "Diagnoses");

            migrationBuilder.DropColumn(
                name: "OralMucosaCondition",
                table: "Diagnoses");

            migrationBuilder.DropColumn(
                name: "PainOnChewing",
                table: "Diagnoses");

            migrationBuilder.DropColumn(
                name: "Plaque",
                table: "Diagnoses");

            migrationBuilder.DropColumn(
                name: "Tartar",
                table: "Diagnoses");

            migrationBuilder.DropColumn(
                name: "TeethCount",
                table: "Diagnoses");

            migrationBuilder.DropColumn(
                name: "TmjSymptoms",
                table: "Diagnoses");

            migrationBuilder.DropColumn(
                name: "WornOrBrokenTeeth",
                table: "Diagnoses");

            migrationBuilder.AlterColumn<string>(
                name: "MedicalHistory",
                table: "Diagnoses",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Conclusion",
                table: "Diagnoses",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AllergyHistory",
                table: "Diagnoses",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

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
                name: "DentalCondition",
                table: "Diagnoses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DiagnosisCode",
                table: "Diagnoses",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "HeartRate",
                table: "Diagnoses",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Diagnoses",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Temperature",
                table: "Diagnoses",
                type: "numeric",
                nullable: true);
        }
    }
}
