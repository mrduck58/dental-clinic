using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalClinic.API.src.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ResetTestPasswords : Migration
    {
        // BCrypt hash of "1" with workFactor 12 — verified correct
        private const string Hash = "$2a$12$HjRMCxhBTTg7Lo59L/I97u0aNmoLa99C8bs/2cdC/0bYvhEbgy7Ou";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Use LOWER() to catch any casing variation in stored emails
            migrationBuilder.Sql(@$"
                UPDATE ""Users""
                SET ""PasswordHash"" = '{Hash}'
                WHERE LOWER(""Email"") IN (
                    'admin@dentalclinic.com',
                    'owner@dentalclinic.com',
                    'staff@dentalclinic.com',
                    'dentist@dentalclinic.com'
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Password reset is intentionally irreversible for test data
        }
    }
}
