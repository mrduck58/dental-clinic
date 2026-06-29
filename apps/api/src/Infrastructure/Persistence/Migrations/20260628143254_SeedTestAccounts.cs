using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalClinic.API.src.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedTestAccounts : Migration
    {
        // BCrypt hash of test credential "1"
        private const string PasswordHash = "$2a$11$LHCYSIxZakivZAPCHRKLN.wbok97yCXFls9j8naHdWH4lZaDY0.ry";

        private static readonly Guid AdminUserId   = new("a0000000-0000-0000-0000-000000000001");
        private static readonly Guid OwnerUserId   = new("a0000000-0000-0000-0000-000000000002");
        private static readonly Guid StaffUserId   = new("a0000000-0000-0000-0000-000000000003");
        private static readonly Guid DentistUserId = new("a0000000-0000-0000-0000-000000000004");
        private static readonly Guid DentistId     = new("a1000000-0000-0000-0000-000000000001");

        private static readonly DateTimeOffset SeedDate =
            new(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), TimeSpan.Zero);

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Use ON CONFLICT DO NOTHING so re-running is safe if any account already exists
            migrationBuilder.Sql($@"
                INSERT INTO ""Users"" (""Id"", ""Username"", ""Email"", ""PasswordHash"", ""Role"", ""FullName"", ""IsActive"", ""CreatedAt"", ""EmploymentStatus"")
                VALUES
                  ('{AdminUserId}',   'admin',   'admin@dentalclinic.com',   '{PasswordHash}', 'Admin',   'Admin Test',   true, '{SeedDate:O}', 'Active'),
                  ('{OwnerUserId}',   'owner',   'owner@dentalclinic.com',   '{PasswordHash}', 'Owner',   'Owner Test',   true, '{SeedDate:O}', 'Active'),
                  ('{StaffUserId}',   'staff',   'staff@dentalclinic.com',   '{PasswordHash}', 'Staff',   'Staff Test',   true, '{SeedDate:O}', 'Active'),
                  ('{DentistUserId}', 'dentist', 'dentist@dentalclinic.com', '{PasswordHash}', 'Dentist', 'Dentist Test', true, '{SeedDate:O}', 'Active')
                ON CONFLICT (""Email"") DO NOTHING;
            ");

            migrationBuilder.Sql($@"
                INSERT INTO ""Dentists"" (""Id"", ""UserId"", ""FullName"", ""Specialization"", ""ExperienceYears"")
                VALUES ('{DentistId}', '{DentistUserId}', 'Dentist Test', 'Nha khoa tổng quát', 1)
                ON CONFLICT (""Id"") DO NOTHING;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "Dentists", keyColumn: "Id", keyValue: DentistId);

            migrationBuilder.DeleteData(table: "Users", keyColumn: "Id", keyValue: DentistUserId);
            migrationBuilder.DeleteData(table: "Users", keyColumn: "Id", keyValue: StaffUserId);
            migrationBuilder.DeleteData(table: "Users", keyColumn: "Id", keyValue: OwnerUserId);
            migrationBuilder.DeleteData(table: "Users", keyColumn: "Id", keyValue: AdminUserId);
        }
    }
}
