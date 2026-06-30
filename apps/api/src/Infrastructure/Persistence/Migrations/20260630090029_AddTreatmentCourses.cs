using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalClinic.API.src.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTreatmentCourses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CourseId",
                table: "Invoices",
                type: "uuid",
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
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    DentistId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    TotalCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
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
                name: "IX_Invoices_CourseId",
                table: "Invoices",
                column: "CourseId");

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_TreatmentCourses_CourseId",
                table: "Appointments");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_TreatmentCourses_CourseId",
                table: "Invoices");

            migrationBuilder.DropTable(
                name: "TreatmentCourses");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_CourseId",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_CourseId",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "CourseId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "CourseId",
                table: "Appointments");
        }
    }
}
