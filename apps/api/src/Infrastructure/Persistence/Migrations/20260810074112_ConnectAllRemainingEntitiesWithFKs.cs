using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalClinic.API.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ConnectAllRemainingEntitiesWithFKs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "EmployeeId",
                table: "SupplyTransactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DentistId",
                table: "MaterialRequests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "AiUsageLogs",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplyTransactions_EmployeeId",
                table: "SupplyTransactions",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialRequests_DentistId",
                table: "MaterialRequests",
                column: "DentistId");

            migrationBuilder.CreateIndex(
                name: "IX_AiUsageLogs_UserId",
                table: "AiUsageLogs",
                column: "UserId");

            migrationBuilder.Sql(@"
                UPDATE ""ActivityLogs"" SET ""UserId"" = NULL WHERE ""UserId"" IS NOT NULL AND ""UserId"" NOT IN (SELECT ""Id"" FROM ""Users"");
                DELETE FROM ""Notifications"" WHERE ""UserId"" NOT IN (SELECT ""Id"" FROM ""Users"");
                DELETE FROM ""ChatConversations"" WHERE ""PatientId"" NOT IN (SELECT ""Id"" FROM ""Patients"");
            ");

            migrationBuilder.AddForeignKey(
                name: "FK_ActivityLogs_Users_UserId",
                table: "ActivityLogs",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AiUsageLogs_Users_UserId",
                table: "AiUsageLogs",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ChatConversations_Patients_PatientId",
                table: "ChatConversations",
                column: "PatientId",
                principalTable: "Patients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialRequests_DentistProfiles_DentistId",
                table: "MaterialRequests",
                column: "DentistId",
                principalTable: "DentistProfiles",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_Users_UserId",
                table: "Notifications",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SupplyTransactions_Employees_EmployeeId",
                table: "SupplyTransactions",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ActivityLogs_Users_UserId",
                table: "ActivityLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_AiUsageLogs_Users_UserId",
                table: "AiUsageLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_ChatConversations_Patients_PatientId",
                table: "ChatConversations");

            migrationBuilder.DropForeignKey(
                name: "FK_MaterialRequests_DentistProfiles_DentistId",
                table: "MaterialRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_Users_UserId",
                table: "Notifications");

            migrationBuilder.DropForeignKey(
                name: "FK_SupplyTransactions_Employees_EmployeeId",
                table: "SupplyTransactions");

            migrationBuilder.DropIndex(
                name: "IX_SupplyTransactions_EmployeeId",
                table: "SupplyTransactions");

            migrationBuilder.DropIndex(
                name: "IX_MaterialRequests_DentistId",
                table: "MaterialRequests");

            migrationBuilder.DropIndex(
                name: "IX_AiUsageLogs_UserId",
                table: "AiUsageLogs");

            migrationBuilder.DropColumn(
                name: "EmployeeId",
                table: "SupplyTransactions");

            migrationBuilder.DropColumn(
                name: "DentistId",
                table: "MaterialRequests");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "AiUsageLogs");
        }
    }
}
