using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalClinic.API.src.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MergeTreatmentCourseIntoPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Dữ liệu dev: xóa liệu trình cũ (cấu trúc không tương thích) và gỡ liên kết
            // hóa đơn ↔ liệu trình dài hạn cũ trước khi đổi FK sang TreatmentPlans.
            migrationBuilder.Sql("DELETE FROM \"TreatmentPlans\";");
            migrationBuilder.Sql("UPDATE \"Invoices\" SET \"CourseId\" = NULL WHERE \"CourseId\" IS NOT NULL;");

            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_TreatmentCourses_CourseId",
                table: "Appointments");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_TreatmentCourses_CourseId",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_TreatmentPlans_Appointments_AppointmentId",
                table: "TreatmentPlans");

            migrationBuilder.DropTable(
                name: "TreatmentCourses");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_CourseId",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "TreatmentPlans");

            migrationBuilder.DropColumn(
                name: "EstimatedCost",
                table: "TreatmentPlans");

            migrationBuilder.DropColumn(
                name: "CourseId",
                table: "Appointments");

            migrationBuilder.RenameColumn(
                name: "CourseId",
                table: "Invoices",
                newName: "TreatmentPlanId");

            migrationBuilder.RenameIndex(
                name: "IX_Invoices_CourseId",
                table: "Invoices",
                newName: "IX_Invoices_TreatmentPlanId");

            migrationBuilder.AlterColumn<Guid>(
                name: "AppointmentId",
                table: "TreatmentPlans",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CompletedAt",
                table: "TreatmentPlans",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DentistId",
                table: "TreatmentPlans",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "TreatmentPlans",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PatientId",
                table: "TreatmentPlans",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "TreatmentPlans",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "ServiceId",
                table: "TreatmentPlans",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "StepProgressJson",
                table: "TreatmentPlans",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Teeth",
                table: "TreatmentPlans",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitPrice",
                table: "TreatmentPlans",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateOnly>(
                name: "WarrantyUntil",
                table: "TreatmentPlans",
                type: "date",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TreatmentProcedures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    StepNumber = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    PercentOfTotal = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TreatmentProcedures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TreatmentProcedures_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentPlans_DentistId",
                table: "TreatmentPlans",
                column: "DentistId");

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentPlans_PatientId",
                table: "TreatmentPlans",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentPlans_ServiceId",
                table: "TreatmentPlans",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentProcedures_ServiceId_StepNumber",
                table: "TreatmentProcedures",
                columns: new[] { "ServiceId", "StepNumber" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_TreatmentPlans_TreatmentPlanId",
                table: "Invoices",
                column: "TreatmentPlanId",
                principalTable: "TreatmentPlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_TreatmentPlans_Appointments_AppointmentId",
                table: "TreatmentPlans",
                column: "AppointmentId",
                principalTable: "Appointments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_TreatmentPlans_Dentists_DentistId",
                table: "TreatmentPlans",
                column: "DentistId",
                principalTable: "Dentists",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TreatmentPlans_Patients_PatientId",
                table: "TreatmentPlans",
                column: "PatientId",
                principalTable: "Patients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TreatmentPlans_Services_ServiceId",
                table: "TreatmentPlans",
                column: "ServiceId",
                principalTable: "Services",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_TreatmentPlans_TreatmentPlanId",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_TreatmentPlans_Appointments_AppointmentId",
                table: "TreatmentPlans");

            migrationBuilder.DropForeignKey(
                name: "FK_TreatmentPlans_Dentists_DentistId",
                table: "TreatmentPlans");

            migrationBuilder.DropForeignKey(
                name: "FK_TreatmentPlans_Patients_PatientId",
                table: "TreatmentPlans");

            migrationBuilder.DropForeignKey(
                name: "FK_TreatmentPlans_Services_ServiceId",
                table: "TreatmentPlans");

            migrationBuilder.DropTable(
                name: "TreatmentProcedures");

            migrationBuilder.DropIndex(
                name: "IX_TreatmentPlans_DentistId",
                table: "TreatmentPlans");

            migrationBuilder.DropIndex(
                name: "IX_TreatmentPlans_PatientId",
                table: "TreatmentPlans");

            migrationBuilder.DropIndex(
                name: "IX_TreatmentPlans_ServiceId",
                table: "TreatmentPlans");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "TreatmentPlans");

            migrationBuilder.DropColumn(
                name: "DentistId",
                table: "TreatmentPlans");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "TreatmentPlans");

            migrationBuilder.DropColumn(
                name: "PatientId",
                table: "TreatmentPlans");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "TreatmentPlans");

            migrationBuilder.DropColumn(
                name: "ServiceId",
                table: "TreatmentPlans");

            migrationBuilder.DropColumn(
                name: "StepProgressJson",
                table: "TreatmentPlans");

            migrationBuilder.DropColumn(
                name: "Teeth",
                table: "TreatmentPlans");

            migrationBuilder.DropColumn(
                name: "UnitPrice",
                table: "TreatmentPlans");

            migrationBuilder.DropColumn(
                name: "WarrantyUntil",
                table: "TreatmentPlans");

            migrationBuilder.RenameColumn(
                name: "TreatmentPlanId",
                table: "Invoices",
                newName: "CourseId");

            migrationBuilder.RenameIndex(
                name: "IX_Invoices_TreatmentPlanId",
                table: "Invoices",
                newName: "IX_Invoices_CourseId");

            migrationBuilder.AlterColumn<Guid>(
                name: "AppointmentId",
                table: "TreatmentPlans",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "TreatmentPlans",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "EstimatedCost",
                table: "TreatmentPlans",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CourseId",
                table: "Appointments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TreatmentCourses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DentistId = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TotalCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TreatmentCourses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TreatmentCourses_Dentists_DentistId",
                        column: x => x.DentistId,
                        principalTable: "Dentists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TreatmentCourses_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_CourseId",
                table: "Appointments",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentCourses_DentistId",
                table: "TreatmentCourses",
                column: "DentistId");

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentCourses_PatientId",
                table: "TreatmentCourses",
                column: "PatientId");

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_TreatmentCourses_CourseId",
                table: "Appointments",
                column: "CourseId",
                principalTable: "TreatmentCourses",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_TreatmentCourses_CourseId",
                table: "Invoices",
                column: "CourseId",
                principalTable: "TreatmentCourses",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_TreatmentPlans_Appointments_AppointmentId",
                table: "TreatmentPlans",
                column: "AppointmentId",
                principalTable: "Appointments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
