using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalClinic.API.src.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedDoctors : Migration
    {
        // Password: Doctor@123
        private const string PasswordHash =
            "$2a$11$Jeecfef6gGkNsUJ/pXjfJ.VuJaQo9rgEunIJ52ob.fVZwdqFOY6V6";

        private const string AvatarBase =
            "https://iyuwmzlolzsdqcucgufr.supabase.co/storage/v1/object/public/avatars";

        private static readonly Guid U1 = new("d1000000-0000-0000-0000-000000000001");
        private static readonly Guid U2 = new("d1000000-0000-0000-0000-000000000002");
        private static readonly Guid U3 = new("d1000000-0000-0000-0000-000000000003");
        private static readonly Guid U4 = new("d1000000-0000-0000-0000-000000000004");

        private static readonly Guid D1 = new("d2000000-0000-0000-0000-000000000001");
        private static readonly Guid D2 = new("d2000000-0000-0000-0000-000000000002");
        private static readonly Guid D3 = new("d2000000-0000-0000-0000-000000000003");
        private static readonly Guid D4 = new("d2000000-0000-0000-0000-000000000004");

        private static readonly DateTimeOffset SeedDate =
            new(new DateTime(2026, 1, 1), TimeSpan.Zero);

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Seed Users (role = doctor) ────────────────────────────────────
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[]
                {
                    "Id", "Username", "Email", "PasswordHash", "Role",
                    "FullName", "PhoneNumber", "IsActive", "CreatedAt",
                    "EmploymentStatus", "ProfilePictureUrl",
                    "Specialty", "YearsOfExperience", "Position",
                    "Department", "Education", "Bio"
                },
                values: new object[,]
                {
                    {
                        U1, "bs.hung", "hung.nguyen@dentalclinic.vn", PasswordHash, "doctor",
                        "BS. Nguyễn Văn Hùng", "0901000001", true, SeedDate,
                        "Active", $"{AvatarBase}/bac_si_1.png",
                        "Nha khoa tổng quát", 10, "Bác sĩ nha khoa",
                        "Nha khoa", "Đại học Y Dược TP.HCM",
                        "Chuyên gia nha khoa tổng quát với hơn 10 năm kinh nghiệm điều trị các bệnh lý răng miệng."
                    },
                    {
                        U2, "bsck2.lan", "lan.tran@dentalclinic.vn", PasswordHash, "doctor",
                        "BSCKII. Trần Thị Lan", "0901000002", true, SeedDate,
                        "Active", $"{AvatarBase}/bac_si_2.png",
                        "Nha khoa thẩm mỹ", 15, "Bác sĩ chuyên khoa II",
                        "Nha khoa thẩm mỹ", "Đại học Y Hà Nội - Chuyên khoa II",
                        "Chuyên gia hàng đầu về nha khoa thẩm mỹ, tẩy trắng răng và phục hình sứ."
                    },
                    {
                        U3, "ths.tuan", "tuan.le@dentalclinic.vn", PasswordHash, "doctor",
                        "ThS.BS. Lê Minh Tuấn", "0901000003", true, SeedDate,
                        "Active", $"{AvatarBase}/bac_si_3.png",
                        "Chỉnh nha", 8, "Thạc sĩ Bác sĩ",
                        "Chỉnh nha", "Đại học Y Dược TP.HCM - Thạc sĩ Chỉnh nha",
                        "Chuyên điều trị sai lệch khớp cắn, niềng răng và chỉnh hình xương hàm."
                    },
                    {
                        U4, "bs.hoa", "hoa.pham@dentalclinic.vn", PasswordHash, "doctor",
                        "BS. Phạm Thị Hoa", "0901000004", true, SeedDate,
                        "Active", $"{AvatarBase}/bac_si_4.png",
                        "Nha nhi", 6, "Bác sĩ nha khoa",
                        "Nha nhi", "Đại học Y Dược Cần Thơ",
                        "Bác sĩ nha nhi tận tâm, chuyên chăm sóc sức khoẻ răng miệng cho trẻ em từ 2–15 tuổi."
                    },
                });

            // ── Seed Dentists ─────────────────────────────────────────────────
            migrationBuilder.InsertData(
                table: "Dentists",
                columns: new[] { "Id", "UserId", "FullName", "Specialization", "ExperienceYears", "Biography" },
                values: new object[,]
                {
                    {
                        D1, U1, "BS. Nguyễn Văn Hùng", "Nha khoa tổng quát", 10,
                        "Điều trị sâu răng, viêm lợi, nhổ răng, trám răng và tư vấn vệ sinh răng miệng."
                    },
                    {
                        D2, U2, "BSCKII. Trần Thị Lan", "Nha khoa thẩm mỹ", 15,
                        "Tẩy trắng răng, dán sứ Veneer, bọc răng sứ và phục hình nha khoa thẩm mỹ cao cấp."
                    },
                    {
                        D3, U3, "ThS.BS. Lê Minh Tuấn", "Chỉnh nha", 8,
                        "Niềng răng mắc cài kim loại, niềng trong suốt Invisalign, chỉnh hình xương hàm."
                    },
                    {
                        D4, U4, "BS. Phạm Thị Hoa", "Nha nhi", 6,
                        "Khám và điều trị răng miệng cho trẻ em, trám răng sữa, nhổ răng sữa và phòng ngừa sâu răng."
                    },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "Dentists", keyColumn: "Id", keyValue: D1);
            migrationBuilder.DeleteData(table: "Dentists", keyColumn: "Id", keyValue: D2);
            migrationBuilder.DeleteData(table: "Dentists", keyColumn: "Id", keyValue: D3);
            migrationBuilder.DeleteData(table: "Dentists", keyColumn: "Id", keyValue: D4);

            migrationBuilder.DeleteData(table: "Users", keyColumn: "Id", keyValue: U1);
            migrationBuilder.DeleteData(table: "Users", keyColumn: "Id", keyValue: U2);
            migrationBuilder.DeleteData(table: "Users", keyColumn: "Id", keyValue: U3);
            migrationBuilder.DeleteData(table: "Users", keyColumn: "Id", keyValue: U4);
        }
    }
}
