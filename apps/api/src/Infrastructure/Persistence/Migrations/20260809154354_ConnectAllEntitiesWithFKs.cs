using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalClinic.API.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ConnectAllEntitiesWithFKs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "EmployeeId",
                table: "WorkSchedules",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RoomId",
                table: "WorkSchedules",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ClinicInfoId",
                table: "Rooms",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MedicineId",
                table: "PrescriptionItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "OtpCodes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SupplyItemId",
                table: "MaterialRequestItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PromotionId",
                table: "Invoices",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkSchedules_EmployeeId",
                table: "WorkSchedules",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkSchedules_RoomId",
                table: "WorkSchedules",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_ClinicInfoId",
                table: "Rooms",
                column: "ClinicInfoId");

            migrationBuilder.CreateIndex(
                name: "IX_PrescriptionItems_MedicineId",
                table: "PrescriptionItems",
                column: "MedicineId");

            migrationBuilder.CreateIndex(
                name: "IX_OtpCodes_UserId",
                table: "OtpCodes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialRequestItems_SupplyItemId",
                table: "MaterialRequestItems",
                column: "SupplyItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_PromotionId",
                table: "Invoices",
                column: "PromotionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Promotions_PromotionId",
                table: "Invoices",
                column: "PromotionId",
                principalTable: "Promotions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialRequestItems_SupplyItems_SupplyItemId",
                table: "MaterialRequestItems",
                column: "SupplyItemId",
                principalTable: "SupplyItems",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_OtpCodes_Users_UserId",
                table: "OtpCodes",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PrescriptionItems_Medicines_MedicineId",
                table: "PrescriptionItems",
                column: "MedicineId",
                principalTable: "Medicines",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Rooms_ClinicInfos_ClinicInfoId",
                table: "Rooms",
                column: "ClinicInfoId",
                principalTable: "ClinicInfos",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkSchedules_Employees_EmployeeId",
                table: "WorkSchedules",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkSchedules_Rooms_RoomId",
                table: "WorkSchedules",
                column: "RoomId",
                principalTable: "Rooms",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Promotions_PromotionId",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_MaterialRequestItems_SupplyItems_SupplyItemId",
                table: "MaterialRequestItems");

            migrationBuilder.DropForeignKey(
                name: "FK_OtpCodes_Users_UserId",
                table: "OtpCodes");

            migrationBuilder.DropForeignKey(
                name: "FK_PrescriptionItems_Medicines_MedicineId",
                table: "PrescriptionItems");

            migrationBuilder.DropForeignKey(
                name: "FK_Rooms_ClinicInfos_ClinicInfoId",
                table: "Rooms");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkSchedules_Employees_EmployeeId",
                table: "WorkSchedules");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkSchedules_Rooms_RoomId",
                table: "WorkSchedules");

            migrationBuilder.DropIndex(
                name: "IX_WorkSchedules_EmployeeId",
                table: "WorkSchedules");

            migrationBuilder.DropIndex(
                name: "IX_WorkSchedules_RoomId",
                table: "WorkSchedules");

            migrationBuilder.DropIndex(
                name: "IX_Rooms_ClinicInfoId",
                table: "Rooms");

            migrationBuilder.DropIndex(
                name: "IX_PrescriptionItems_MedicineId",
                table: "PrescriptionItems");

            migrationBuilder.DropIndex(
                name: "IX_OtpCodes_UserId",
                table: "OtpCodes");

            migrationBuilder.DropIndex(
                name: "IX_MaterialRequestItems_SupplyItemId",
                table: "MaterialRequestItems");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_PromotionId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "EmployeeId",
                table: "WorkSchedules");

            migrationBuilder.DropColumn(
                name: "RoomId",
                table: "WorkSchedules");

            migrationBuilder.DropColumn(
                name: "ClinicInfoId",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "MedicineId",
                table: "PrescriptionItems");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "OtpCodes");

            migrationBuilder.DropColumn(
                name: "SupplyItemId",
                table: "MaterialRequestItems");

            migrationBuilder.DropColumn(
                name: "PromotionId",
                table: "Invoices");
        }
    }
}
