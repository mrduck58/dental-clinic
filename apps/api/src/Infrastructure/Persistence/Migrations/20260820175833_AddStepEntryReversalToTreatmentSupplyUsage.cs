using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalClinic.API.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStepEntryReversalToTreatmentSupplyUsage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsReversed",
                table: "TreatmentSupplyUsages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "StepEntryId",
                table: "TreatmentSupplyUsages",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentSupplyUsages_StepEntryId",
                table: "TreatmentSupplyUsages",
                column: "StepEntryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TreatmentSupplyUsages_StepEntryId",
                table: "TreatmentSupplyUsages");

            migrationBuilder.DropColumn(
                name: "IsReversed",
                table: "TreatmentSupplyUsages");

            migrationBuilder.DropColumn(
                name: "StepEntryId",
                table: "TreatmentSupplyUsages");
        }
    }
}
