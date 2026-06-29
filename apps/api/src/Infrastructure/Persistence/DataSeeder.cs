using DentalClinic.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Infrastructure.Persistence;

public static class DataSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        await SeedSupplyItemsAsync(db);
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
