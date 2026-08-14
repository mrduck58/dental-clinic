using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalClinic.API.Infrastructure.src.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUnitToServiceOption : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "ServiceOptions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Răng");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Unit",
                table: "ServiceOptions");
        }
    }
}
