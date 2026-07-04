using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalClinic.API.src.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPatientFamilyRelations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PrimaryPatientId",
                table: "Patients",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProfilePictureUrl",
                table: "Patients",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Relationship",
                table: "Patients",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Patients_PrimaryPatientId",
                table: "Patients",
                column: "PrimaryPatientId");

            migrationBuilder.AddForeignKey(
                name: "FK_Patients_Patients_PrimaryPatientId",
                table: "Patients",
                column: "PrimaryPatientId",
                principalTable: "Patients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Patients_Patients_PrimaryPatientId",
                table: "Patients");

            migrationBuilder.DropIndex(
                name: "IX_Patients_PrimaryPatientId",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "PrimaryPatientId",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "ProfilePictureUrl",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "Relationship",
                table: "Patients");
        }
    }
}
