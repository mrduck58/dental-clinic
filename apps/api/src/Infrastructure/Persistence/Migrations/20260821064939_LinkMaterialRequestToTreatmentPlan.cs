using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalClinic.API.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LinkMaterialRequestToTreatmentPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TreatmentPlanId",
                table: "MaterialRequests",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaterialRequests_TreatmentPlanId",
                table: "MaterialRequests",
                column: "TreatmentPlanId");

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialRequests_TreatmentPlans_TreatmentPlanId",
                table: "MaterialRequests",
                column: "TreatmentPlanId",
                principalTable: "TreatmentPlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaterialRequests_TreatmentPlans_TreatmentPlanId",
                table: "MaterialRequests");

            migrationBuilder.DropIndex(
                name: "IX_MaterialRequests_TreatmentPlanId",
                table: "MaterialRequests");

            migrationBuilder.DropColumn(
                name: "TreatmentPlanId",
                table: "MaterialRequests");
        }
    }
}
