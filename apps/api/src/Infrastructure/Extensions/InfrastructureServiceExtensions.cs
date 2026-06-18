using DentalClinic.API.Application.UseCases.Auth;
using DentalClinic.API.Application.UseCases.Medicines;
using DentalClinic.API.Application.UseCases.Feedbacks;
using DentalClinic.API.Application.UseCases.Posts;
using DentalClinic.API.Application.UseCases.Promotions;
using DentalClinic.API.Application.UseCases.Rooms;
using DentalClinic.API.Application.UseCases.Schedules;
using DentalClinic.API.Application.UseCases.Services;
using DentalClinic.API.Application.UseCases.Staff;
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
        services.AddScoped<IServiceRepository, ServiceRepository>();
        services.AddScoped<IPostRepository, PostRepository>();
        services.AddScoped<IFeedbackRepository, FeedbackRepository>();
        services.AddScoped<IWorkScheduleRepository, WorkScheduleRepository>();
        services.AddScoped<IPromotionRepository, PromotionRepository>();
        services.AddScoped<IRoomRepository, RoomRepository>();
        services.AddScoped<IMedicineRepository, MedicineRepository>();

        // ── Services ────────────────────────────────────────────────────────
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IFileStorageService, LocalFileStorageService>();

        // ── Use Case Handlers ────────────────────────────────────────────────
        services.AddScoped<LoginHandler>();
        services.AddScoped<CreateAccountHandler>();
        services.AddScoped<GetAccountsHandler>();

        services.AddScoped<GetStaffHandler>();
        services.AddScoped<CreateStaffHandler>();
        services.AddScoped<UpdateStaffHandler>();
        services.AddScoped<ResetStaffPasswordHandler>();
        services.AddScoped<CreateStaffAccountHandler>();

        services.AddScoped<GetFeedbacksHandler>();
        services.AddScoped<GetFeedbackByIdHandler>();
        services.AddScoped<CreateFeedbackHandler>();
        services.AddScoped<ApproveFeedbackHandler>();
        services.AddScoped<HideFeedbackHandler>();
        services.AddScoped<ReplyFeedbackHandler>();

        services.AddScoped<GetPostsHandler>();
        services.AddScoped<GetPostByIdHandler>();
        services.AddScoped<CreatePostHandler>();
        services.AddScoped<UpdatePostHandler>();
        services.AddScoped<DeletePostHandler>();

        services.AddScoped<GetServicesHandler>();
        services.AddScoped<GetServiceByIdHandler>();
        services.AddScoped<CreateServiceHandler>();
        services.AddScoped<UpdateServiceHandler>();
        services.AddScoped<DeleteServiceHandler>();
        services.AddScoped<ToggleServiceStatusHandler>();

        services.AddScoped<GetMedicinesHandler>();
        services.AddScoped<GetMedicineByIdHandler>();
        services.AddScoped<CreateMedicineHandler>();
        services.AddScoped<UpdateMedicineHandler>();
        services.AddScoped<DeleteMedicineHandler>();

        services.AddScoped<GetWeekScheduleHandler>();
        services.AddScoped<SaveWeekScheduleHandler>();

        services.AddScoped<GetPromotionsHandler>();
        services.AddScoped<GetPromotionByIdHandler>();
        services.AddScoped<CreatePromotionHandler>();
        services.AddScoped<UpdatePromotionHandler>();
        services.AddScoped<DeletePromotionHandler>();
        services.AddScoped<TogglePromotionStatusHandler>();

        services.AddScoped<GetRoomsHandler>();
        services.AddScoped<GetRoomByIdHandler>();
        services.AddScoped<CreateRoomHandler>();
        services.AddScoped<UpdateRoomHandler>();
        services.AddScoped<DeleteRoomHandler>();
        services.AddScoped<ChangeRoomStatusHandler>();

        return services;
    }
}
