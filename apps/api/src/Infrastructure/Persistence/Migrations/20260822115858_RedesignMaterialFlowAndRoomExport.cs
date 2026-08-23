using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalClinic.API.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RedesignMaterialFlowAndRoomExport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RoomId",
                table: "SupplyTransactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplyTransactions_RoomId",
                table: "SupplyTransactions",
                column: "RoomId");

            migrationBuilder.AddForeignKey(
                name: "FK_SupplyTransactions_Rooms_RoomId",
                table: "SupplyTransactions",
                column: "RoomId",
                principalTable: "Rooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SupplyTransactions_Rooms_RoomId",
                table: "SupplyTransactions");

            migrationBuilder.DropIndex(
                name: "IX_SupplyTransactions_RoomId",
                table: "SupplyTransactions");

            migrationBuilder.DropColumn(
                name: "RoomId",
                table: "SupplyTransactions");
        }
    }
}
