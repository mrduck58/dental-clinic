using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalClinic.API.src.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDuplicateFullName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Patients_Users_UserId",
                table: "Patients");

            // 1. Update Patients that don't have a UserId to use their Id as UserId
            migrationBuilder.Sql("UPDATE \"Patients\" SET \"UserId\" = \"Id\" WHERE \"UserId\" IS NULL;");

            // 2. Insert placeholder User records for those patients
            migrationBuilder.Sql(@"
                INSERT INTO ""Users"" (""Id"", ""Email"", ""Role"", ""FullName"", ""PhoneNumber"", ""CreatedAt"", ""IsActive"")
                SELECT 
                    p.""Id"",
                    'placeholder_' || CAST(p.""Id"" AS text) || '@songiangdental.com',
                    'Patient',
                    COALESCE(p.""FullName"", 'Bệnh nhân chưa đặt tên'),
                    p.""PhoneNumber"",
                    NOW(),
                    true
                FROM ""Patients"" p
                WHERE NOT EXISTS (SELECT 1 FROM ""Users"" u WHERE u.""Id"" = p.""UserId"");
            ");

            // 3. Copy FullName from Patients/Dentists to Users if missing
            migrationBuilder.Sql(@"
                UPDATE ""Users"" u
                SET ""FullName"" = p.""FullName""
                FROM ""Patients"" p
                WHERE u.""Id"" = p.""UserId"" AND (u.""FullName"" IS NULL OR u.""FullName"" = '');
            ");

            migrationBuilder.Sql(@"
                UPDATE ""Users"" u
                SET ""FullName"" = d.""FullName""
                FROM ""Dentists"" d
                WHERE u.""Id"" = d.""UserId"" AND (u.""FullName"" IS NULL OR u.""FullName"" = '');
            ");

            migrationBuilder.DropColumn(
                name: "FullName",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "FullName",
                table: "Dentists");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "Patients",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Patients_Users_UserId",
                table: "Patients",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Patients_Users_UserId",
                table: "Patients");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "Patients",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "FullName",
                table: "Patients",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FullName",
                table: "Dentists",
                type: "text",
                nullable: false,
                defaultValue: "");

            // Populate FullName back from Users
            migrationBuilder.Sql(@"
                UPDATE ""Patients"" p
                SET ""FullName"" = COALESCE(u.""FullName"", '')
                FROM ""Users"" u
                WHERE p.""UserId"" = u.""Id"";
            ");

            migrationBuilder.Sql(@"
                UPDATE ""Dentists"" d
                SET ""FullName"" = COALESCE(u.""FullName"", '')
                FROM ""Users"" u
                WHERE d.""UserId"" = u.""Id"";
            ");

            migrationBuilder.AddForeignKey(
                name: "FK_Patients_Users_UserId",
                table: "Patients",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");
        }
    }
}
