using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalClinic.API.src.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGoogleAuthAndOtpPurpose : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OtpCodes_Email_IsUsed_ExpiresAt",
                table: "OtpCodes");

            migrationBuilder.AddColumn<string>(
                name: "Provider",
                table: "Users",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Purpose",
                table: "OtpCodes",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Registration");

            migrationBuilder.CreateIndex(
                name: "IX_OtpCodes_Email_Purpose_IsUsed_ExpiresAt",
                table: "OtpCodes",
                columns: new[] { "Email", "Purpose", "IsUsed", "ExpiresAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OtpCodes_Email_Purpose_IsUsed_ExpiresAt",
                table: "OtpCodes");

            migrationBuilder.DropColumn(
                name: "Provider",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Purpose",
                table: "OtpCodes");

            migrationBuilder.CreateIndex(
                name: "IX_OtpCodes_Email_IsUsed_ExpiresAt",
                table: "OtpCodes",
                columns: new[] { "Email", "IsUsed", "ExpiresAt" });
        }
    }
}
