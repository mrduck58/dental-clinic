using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalClinic.API.src.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnsureTestAccountPasswords : Migration
    {
        // BCrypt hash of test credential "1"
        private const string PasswordHash = "$2a$11$LHCYSIxZakivZAPCHRKLN.wbok97yCXFls9j8naHdWH4lZaDY0.ry";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Force all test accounts to password "1" regardless of prior value
            migrationBuilder.Sql($@"
                UPDATE ""Users""
                SET ""PasswordHash"" = '{PasswordHash}'
                WHERE ""Email"" IN (
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
        }
    }
}
