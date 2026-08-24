using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalClinic.API.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDentistProfileContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Content",
                table: "DentistProfiles",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Content",
                table: "DentistProfiles");
        }
    }
}
