using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Infrastructure.Persistence;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        await SeedUsersAsync(db);
        await SeedRoomsAsync(db);
    }

    private static async Task SeedUsersAsync(AppDbContext db)
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

    private static async Task SeedRoomsAsync(AppDbContext db)
    {
        if (await db.Rooms.AnyAsync())
            return;

        var rooms = new[]
        {
            Room.Create("P001", "Phòng 1", "1", "Khám tổng quát",
                "Phòng khám tổng quát đầy đủ trang thiết bị nha khoa, ghế khám cơ học thế hệ mới, đèn LED chiếu sáng chuyên dụng."),

            Room.Create("P002", "Phòng 2", "1", "Khám tổng quát",
                "Phòng khám nha khoa tổng quát cơ bản, trang bị dụng cụ chẩn đoán hình ảnh di động và hệ thống hút nước."),

            Room.Create("P003", "Phòng 3", "2", "Phẫu thuật",
                "Phòng phẫu thuật nha khoa chuyên sâu, hệ thống vô khuẩn cấp độ cao, dụng cụ phẫu thuật implant và nhổ răng khôn."),

            Room.Create("P004", "Phòng 4", "2", "X-Quang",
                "Phòng X-Quang kỹ thuật số hiện đại (Digital Radiography), hỗ trợ chụp phim toàn cảnh (Panorama) và CT-Cone Beam."),

            Room.Create("P005", "Phòng 5", "3", "Cấp cứu",
                "Phòng cấp cứu nha khoa, trang bị đầy đủ thiết bị xử lý các trường hợp khẩn cấp: đau tủy cấp, chấn thương răng, áp-xe."),
        };

        // Đặt trạng thái thực tế cho từng phòng
        rooms[0].ChangeStatus(RoomStatus.DangKham);    // P001 đang có bệnh nhân
        rooms[2].ChangeStatus(RoomStatus.BaoTri);      // P003 đang bảo trì
        // P002, P004, P005 mặc định là Trống

        db.Rooms.AddRange(rooms);
        await db.SaveChangesAsync();

        Console.WriteLine("─────────────────────────────────────────────");
        Console.WriteLine("  [SEED] Đã tạo 5 phòng khám mẫu:");
        foreach (var r in rooms)
            Console.WriteLine($"  {r.Code} - {r.Name} ({r.Type}) | {r.Status}");
        Console.WriteLine("─────────────────────────────────────────────");
    }
}
