using DentalClinic.API.Application.UseCases.Auth;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using DentalClinic.API.Infrastructure.Persistence;
using DentalClinic.API.Infrastructure.Persistence.Repositories;
using DentalClinic.API.Infrastructure.Services;
using DentalClinic.API.Infrastructure.Settings;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Infrastructure.Extensions;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── Database ────────────────────────────────────────────────────────
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        // ── Settings ────────────────────────────────────────────────────────
        services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));
        services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));

        // ── Repositories ────────────────────────────────────────────────────
        services.AddScoped<IAppointmentRepository, AppointmentRepository>();
        services.AddScoped<IPatientRepository, PatientRepository>();
        services.AddScoped<IUserRepository, UserRepository>();

        // ── Services ────────────────────────────────────────────────────────
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IEmailService, EmailService>();

        // ── Use Case Handlers ────────────────────────────────────────────────
        services.AddScoped<LoginHandler>();
        services.AddScoped<CreateAccountHandler>();
        services.AddScoped<GetAccountsHandler>();

        return services;
    }
}
