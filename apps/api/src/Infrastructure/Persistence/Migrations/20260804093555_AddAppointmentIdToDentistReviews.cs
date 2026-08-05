using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalClinic.API.src.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAppointmentIdToDentistReviews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DentistReviews_DentistId_PatientId",
                table: "DentistReviews");

            migrationBuilder.AddColumn<Guid>(
                name: "AppointmentId",
                table: "DentistReviews",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DentistReviews_AppointmentId",
                table: "DentistReviews",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_DentistReviews_DentistId_PatientId",
                table: "DentistReviews",
                columns: new[] { "DentistId", "PatientId" });

            migrationBuilder.AddForeignKey(
                name: "FK_DentistReviews_Appointments_AppointmentId",
                table: "DentistReviews",
                column: "AppointmentId",
                principalTable: "Appointments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DentistReviews_Appointments_AppointmentId",
                table: "DentistReviews");

            migrationBuilder.DropIndex(
                name: "IX_DentistReviews_AppointmentId",
                table: "DentistReviews");

            migrationBuilder.DropIndex(
                name: "IX_DentistReviews_DentistId_PatientId",
                table: "DentistReviews");

            migrationBuilder.DropColumn(
                name: "AppointmentId",
                table: "DentistReviews");

            migrationBuilder.CreateIndex(
                name: "IX_DentistReviews_DentistId_PatientId",
                table: "DentistReviews",
                columns: new[] { "DentistId", "PatientId" },
                unique: true);
        }
    }
}
