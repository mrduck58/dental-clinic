using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalClinic.API.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMaterialRequestProcurementWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "OrderedAt",
                table: "MaterialRequests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrderedBy",
                table: "MaterialRequests",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupplierNote",
                table: "MaterialRequests",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ActualQuantity",
                table: "MaterialRequestItems",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OrderedAt",
                table: "MaterialRequests");

            migrationBuilder.DropColumn(
                name: "OrderedBy",
                table: "MaterialRequests");

            migrationBuilder.DropColumn(
                name: "SupplierNote",
                table: "MaterialRequests");

            migrationBuilder.DropColumn(
                name: "ActualQuantity",
                table: "MaterialRequestItems");
        }
    }
}
