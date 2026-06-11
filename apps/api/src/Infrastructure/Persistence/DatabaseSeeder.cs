using DentalClinic.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Infrastructure.Persistence;

public static class DatabaseSeeder
{
    /// <summary>
    /// Tạo tài khoản Admin mặc định nếu DB chưa có user nào.
    /// Chỉ chạy 1 lần khi khởi động lần đầu.
    /// </summary>
    public static async Task SeedAsync(AppDbContext db)
    {
        if (await db.Users.AnyAsync())
            return;

        const string defaultPassword = "Admin@123456";
        var hash = BCrypt.Net.BCrypt.HashPassword(defaultPassword, workFactor: 12);

        var admin = User.Create(
            username:     "admin",
            email:        "admin@dentalclinic.com",
            passwordHash: hash,
            role:         "Admin",
            phoneNumber:  null);

        db.Users.Add(admin);
        await db.SaveChangesAsync();

        Console.WriteLine("─────────────────────────────────────────────");
        Console.WriteLine("  [SEED] Tài khoản Admin mặc định đã tạo:");
        Console.WriteLine($"  Email:    admin@dentalclinic.com");
        Console.WriteLine($"  Password: {defaultPassword}");
        Console.WriteLine("  ⚠️  Đổi mật khẩu ngay sau khi đăng nhập!");
        Console.WriteLine("─────────────────────────────────────────────");
    }
}
