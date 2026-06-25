using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalClinic.API.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceIdToPost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ServiceId",
                table: "Posts",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Posts_ServiceId",
                table: "Posts",
                column: "ServiceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Posts_Services_ServiceId",
                table: "Posts",
                column: "ServiceId",
                principalTable: "Services",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Posts_Services_ServiceId",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_Posts_ServiceId",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "ServiceId",
                table: "Posts");
        }
    }
}
