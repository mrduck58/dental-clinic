using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DentalClinic.API.Infrastructure.Persistence;

/// <summary>
/// Chỉ dùng cho `dotnet ef` lúc thiết kế (tạo/gỡ migration), không chạy lúc ứng dụng chạy thật.
///
/// Không có lớp này thì `dotnet ef` phải dựng DbContext qua project host DentalClinic.API. Khi API
/// đang chạy, thư mục output của host bị khóa nên build hụt, và `--no-build` lại đọc đúng đống DLL
/// CŨ đó — kết quả là migration sinh ra RỖNG mà không báo lỗi gì, rất dễ commit nhầm rồi tưởng
/// đã có cột trong DB.
///
/// Có lớp này thì chạy được:
///   dotnet ef migrations add TenMigration --project src/Infrastructure --startup-project src/Infrastructure
/// Chuỗi kết nối chỉ để EF dựng model — không mở kết nối thật khi sinh migration.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Database=dentalclinic_design_time;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new AppDbContext(options);
    }
}
