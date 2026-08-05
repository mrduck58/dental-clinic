using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Infrastructure.Persistence;

public static class DataSeeder
{
    /// <summary>Mật khẩu mặc định cho các tài khoản seed — đổi ngay sau lần đăng nhập đầu tiên.</summary>
    private const string DefaultSeedPassword = "Admin@123";

    public static async Task SeedAsync(AppDbContext db)
    {
        await SeedSupplyItemsAsync(db);
        await SeedInitialAccountsAsync(db);
    }

    /// <summary>Migration InitialCreate xóa sạch dữ liệu cũ — cần tối thiểu 1 tài khoản mỗi role
    /// quản trị (Admin/Owner) và 1 bác sĩ mẫu để có thể đăng nhập lần đầu sau khi deploy.</summary>
    private static async Task SeedInitialAccountsAsync(AppDbContext db)
    {
        if (await db.Users.AnyAsync()) return;

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(DefaultSeedPassword, workFactor: 12);

        var admin = User.Create("admin", "admin@dentalclinic.local", passwordHash, UserRole.Admin, fullName: "Quản trị viên");
        var owner = User.Create("owner", "owner@dentalclinic.local", passwordHash, UserRole.Owner, fullName: "Chủ phòng khám");
        var dentistUser = User.Create("dentist", "dentist@dentalclinic.local", passwordHash, UserRole.Dentist, fullName: "Bác sĩ mẫu");
        await db.Users.AddRangeAsync(admin, owner, dentistUser);
        await db.SaveChangesAsync();

        var dentistEmployee = Employee.Create(dentistUser.Id, "DT-0001");
        await db.Employees.AddAsync(dentistEmployee);
        await db.SaveChangesAsync();

        var dentistProfile = DentistProfile.Create(dentistEmployee.Id, "Nha khoa tổng quát", "N/A", experienceYears: 0);
        await db.DentistProfiles.AddAsync(dentistProfile);
        await db.SaveChangesAsync();
    }

    private static async Task SeedSupplyItemsAsync(AppDbContext db)
    {
        if (await db.SupplyItems.AnyAsync()) return;

        var items = new[]
        {
            SupplyItem.Create("VT001", "Găng tay latex (M)",       "Bảo hộ",   "Hộp (100c)",   15,  5),
            SupplyItem.Create("VT002", "Khẩu trang y tế",          "Bảo hộ",   "Hộp (50c)",     8,  5),
            SupplyItem.Create("VT003", "Mũi khoan composite",      "Dụng cụ",  "Cái",           45, 20),
            SupplyItem.Create("VT004", "Composite A2",             "Vật liệu", "Tuýp",           6, 10),
            SupplyItem.Create("VT005", "Composite A3",             "Vật liệu", "Tuýp",           3, 10),
            SupplyItem.Create("VT006", "Nước súc miệng Listerine", "Tiêu hao", "Chai 500ml",    12,  6),
            SupplyItem.Create("VT007", "Kim tiêm nha khoa",        "Dụng cụ",  "Hộp (100c)",    4,  5),
            SupplyItem.Create("VT008", "Thuốc tê Lidocaine",       "Thuốc",    "Hộp (50 ống)",  7,  4),
            SupplyItem.Create("VT009", "Giấy cắn khớp",            "Tiêu hao", "Cuộn",          20,  8),
            SupplyItem.Create("VT010", "Bơm rửa nha khoa",         "Dụng cụ",  "Cái",           30, 10),
        };

        await db.SupplyItems.AddRangeAsync(items);
        await db.SaveChangesAsync();
    }
}
