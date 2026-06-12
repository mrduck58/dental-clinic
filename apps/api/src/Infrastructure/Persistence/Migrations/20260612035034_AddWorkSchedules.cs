using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalClinic.API.src.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkSchedules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WorkSchedules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Shift = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    StaffName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Room = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RoomColor = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsHoliday = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkSchedules", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkSchedules_Date",
                table: "WorkSchedules",
                column: "Date");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkSchedules");
        }
    }
}
