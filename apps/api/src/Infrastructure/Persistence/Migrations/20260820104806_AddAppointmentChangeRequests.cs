using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalClinic.API.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAppointmentChangeRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // "UserDeviceTokens" đã bị scaffold trùng vào migration này lúc tạo (đã có sẵn từ migration
            // RenameMaterialRequestCourseIdToPatientId trước đó) — bỏ để tránh lỗi "relation already
            // exists" khi chạy database update từ đầu trên DB trống.
            migrationBuilder.CreateTable(
                name: "AppointmentChangeRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AppointmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DesiredDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DesiredTimeSlot = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    DesiredDentistId = table.Column<Guid>(type: "uuid", nullable: true),
                    StaffNote = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ProcessedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppointmentChangeRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppointmentChangeRequests_Appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "Appointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AppointmentChangeRequests_DentistProfiles_DesiredDentistId",
                        column: x => x.DesiredDentistId,
                        principalTable: "DentistProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AppointmentChangeRequests_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AppointmentChangeRequests_Users_ProcessedByUserId",
                        column: x => x.ProcessedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AppointmentChangeRequests_Users_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AppointmentSlotHolds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DentistId = table.Column<Guid>(type: "uuid", nullable: false),
                    AppointmentDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TimeSlot = table.Column<string>(type: "text", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uuid", nullable: true),
                    DurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    IsSuccess = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppointmentSlotHolds", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentChangeRequests_AppointmentId",
                table: "AppointmentChangeRequests",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentChangeRequests_DesiredDentistId",
                table: "AppointmentChangeRequests",
                column: "DesiredDentistId");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentChangeRequests_PatientId",
                table: "AppointmentChangeRequests",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentChangeRequests_ProcessedByUserId",
                table: "AppointmentChangeRequests",
                column: "ProcessedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentChangeRequests_RequestedByUserId",
                table: "AppointmentChangeRequests",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentChangeRequests_Status",
                table: "AppointmentChangeRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentSlotHolds_DentistId_AppointmentDate",
                table: "AppointmentSlotHolds",
                columns: new[] { "DentistId", "AppointmentDate" });

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentSlotHolds_PatientId_CreatedAt",
                table: "AppointmentSlotHolds",
                columns: new[] { "PatientId", "CreatedAt" });

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppointmentChangeRequests");

            migrationBuilder.DropTable(
                name: "AppointmentSlotHolds");
        }
    }
}
