using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalClinic.API.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameMaterialRequestCourseIdToPatientId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CourseId",
                table: "MaterialRequests",
                newName: "PatientId");

            migrationBuilder.CreateTable(
                name: "ServiceSupplyItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplyItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    DefaultQuantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceSupplyItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceSupplyItems_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ServiceSupplyItems_SupplyItems_SupplyItemId",
                        column: x => x.SupplyItemId,
                        principalTable: "SupplyItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TreatmentSupplyUsages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TreatmentPlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplyItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    UnitCostAtUsage = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    SupplyTransactionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TreatmentSupplyUsages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TreatmentSupplyUsages_SupplyItems_SupplyItemId",
                        column: x => x.SupplyItemId,
                        principalTable: "SupplyItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TreatmentSupplyUsages_SupplyTransactions_SupplyTransactionId",
                        column: x => x.SupplyTransactionId,
                        principalTable: "SupplyTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TreatmentSupplyUsages_TreatmentPlans_TreatmentPlanId",
                        column: x => x.TreatmentPlanId,
                        principalTable: "TreatmentPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserDeviceTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "text", nullable: false),
                    DeviceType = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserDeviceTokens", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceSupplyItems_ServiceId_SupplyItemId",
                table: "ServiceSupplyItems",
                columns: new[] { "ServiceId", "SupplyItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceSupplyItems_SupplyItemId",
                table: "ServiceSupplyItems",
                column: "SupplyItemId");

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentSupplyUsages_SupplyItemId",
                table: "TreatmentSupplyUsages",
                column: "SupplyItemId");

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentSupplyUsages_SupplyTransactionId",
                table: "TreatmentSupplyUsages",
                column: "SupplyTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentSupplyUsages_TreatmentPlanId",
                table: "TreatmentSupplyUsages",
                column: "TreatmentPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_UserDeviceTokens_Token",
                table: "UserDeviceTokens",
                column: "Token");

            migrationBuilder.CreateIndex(
                name: "IX_UserDeviceTokens_UserId",
                table: "UserDeviceTokens",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServiceSupplyItems");

            migrationBuilder.DropTable(
                name: "TreatmentSupplyUsages");

            migrationBuilder.DropTable(
                name: "UserDeviceTokens");

            migrationBuilder.RenameColumn(
                name: "PatientId",
                table: "MaterialRequests",
                newName: "CourseId");
        }
    }
}
