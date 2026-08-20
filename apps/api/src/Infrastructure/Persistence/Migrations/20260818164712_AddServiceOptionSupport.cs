using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalClinic.API.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceOptionSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ServiceSupplyItems_ServiceId_SupplyItemId",
                table: "ServiceSupplyItems");

            migrationBuilder.AddColumn<string>(
                name: "ServiceOptionName",
                table: "TreatmentPlans",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ServiceOptionName",
                table: "ServiceSupplyItems",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceSupplyItems_ServiceId_SupplyItemId_ServiceOptionName",
                table: "ServiceSupplyItems",
                columns: new[] { "ServiceId", "SupplyItemId", "ServiceOptionName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ServiceSupplyItems_ServiceId_SupplyItemId_ServiceOptionName",
                table: "ServiceSupplyItems");

            migrationBuilder.DropColumn(
                name: "ServiceOptionName",
                table: "TreatmentPlans");

            migrationBuilder.DropColumn(
                name: "ServiceOptionName",
                table: "ServiceSupplyItems");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceSupplyItems_ServiceId_SupplyItemId",
                table: "ServiceSupplyItems",
                columns: new[] { "ServiceId", "SupplyItemId" },
                unique: true);
        }
    }
}
