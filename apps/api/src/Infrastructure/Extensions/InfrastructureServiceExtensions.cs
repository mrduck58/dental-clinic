using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Infrastructure.Persistence;
using DentalClinic.API.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Infrastructure.Extensions;

/// <summary>
/// Extension methods để đăng ký toàn bộ service của tầng Infrastructure vào DI Container.
/// Gọi phương thức này trong Program.cs: builder.Services.AddInfrastructure(builder.Configuration)
/// </summary>
public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── Database (EF Core + Npgsql cho PostgreSQL) ──────────────────
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        // ── Repository Pattern ──────────────────────────────────────────
        services.AddScoped<IAppointmentRepository, AppointmentRepository>();
        services.AddScoped<IPatientRepository, PatientRepository>();
        // TODO: Thêm các repository khác tại đây khi triển khai

        return services;
    }
}
