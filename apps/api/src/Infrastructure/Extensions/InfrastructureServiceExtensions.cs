using DentalClinic.API.Application.UseCases.Booking;
using DentalClinic.API.Application.UseCases.Dentists;
using DentalClinic.API.Application.UseCases.Invoices;
using DentalClinic.API.Application.UseCases.Queue;
using DentalClinic.API.Application.UseCases.Patients;
using DentalClinic.API.Application.UseCases.Payments;
using DentalClinic.API.Application.UseCases.Staff;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using DentalClinic.API.Infrastructure.Persistence;
using DentalClinic.API.Infrastructure.Persistence.Repositories;
using DentalClinic.API.Infrastructure.Services;
using DentalClinic.API.Infrastructure.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;

namespace DentalClinic.API.Infrastructure.Extensions;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── Database ────────────────────────────────────────────────────────
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"))
                   .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning)));

        // ── Mediator ────────────────────────────────────────────────────────
        // Auto-discovers IRequestHandler<> implementations by assembly scan, replacing the need to
        // manually register each Handler below one by one. Controllers depend on ISender only.
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(InfrastructureServiceExtensions).Assembly));

        // ── Settings ────────────────────────────────────────────────────────
        // JwtSettings bound once as a fixed singleton instance (not via IOptions<T>): IOptions<T> is
        // invalidated and re-bound whenever IConfiguration's reload token fires (appsettings.json is
        // watched with reloadOnChange:true by default), which let JwtService's signing-side Issuer/
        // Audience drift out of sync with the validation-side values captured once in Program.cs —
        // causing tokens to intermittently be signed without iss/aud and fail audience validation.
        services.AddSingleton(configuration.GetSection("JwtSettings").Get<JwtSettings>() ?? new JwtSettings());
        services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));
        services.Configure<PayOSSettings>(configuration.GetSection("PayOSSettings"));
        services.Configure<GeminiSettings>(configuration.GetSection("GeminiSettings"));
        services.Configure<GoogleAuthSettings>(configuration.GetSection("GoogleAuthSettings"));

        // ── Repositories ────────────────────────────────────────────────────
        services.AddScoped<IAppointmentRepository, AppointmentRepository>();
        services.AddScoped<IPatientRepository, PatientRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IStaffRepository, StaffRepository>();
        services.AddScoped<IOtpRepository, OtpRepository>();
        services.AddScoped<IServiceRepository, ServiceRepository>();
        services.AddScoped<IPostRepository, PostRepository>();
        services.AddScoped<IFeedbackRepository, FeedbackRepository>();
        services.AddScoped<ILeaveRequestRepository, LeaveRequestRepository>();
        services.AddScoped<IWorkScheduleRepository, WorkScheduleRepository>();
        services.AddScoped<IPromotionRepository, PromotionRepository>();
        services.AddScoped<IRoomRepository, RoomRepository>();
        services.AddScoped<IMedicineRepository, MedicineRepository>();
        services.AddScoped<ISupplyItemRepository, SupplyItemRepository>();
        services.AddScoped<ISupplyTransactionRepository, SupplyTransactionRepository>();
        services.AddScoped<IClinicInfoRepository, ClinicInfoRepository>();
        services.AddScoped<IActivityLogRepository, ActivityLogRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IChatConversationRepository, ChatConversationRepository>();
        services.AddScoped<IChatMessageRepository, ChatMessageRepository>();
        services.AddScoped<IAiUsageLogRepository, AiUsageLogRepository>();
        services.AddScoped<IMaterialRequestRepository, MaterialRequestRepository>();
        services.AddScoped<ITreatmentProcedureRepository, TreatmentProcedureRepository>();
        services.AddScoped<IDentistRepository, DentistRepository>();
        services.AddScoped<IDentistReviewRepository, DentistReviewRepository>();
        services.AddScoped<IDiagnosisRepository, DiagnosisRepository>();
        services.AddScoped<IPrescriptionRepository, PrescriptionRepository>();
        services.AddScoped<IPrescriptionItemRepository, PrescriptionItemRepository>();
        services.AddScoped<ITreatmentPlanRepository, TreatmentPlanRepository>();

        // ── Services ────────────────────────────────────────────────────────
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IActivityLogService, ActivityLogService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IFileStorageService, LocalFileStorageService>();
        services.AddScoped<IGoogleAuthService, GoogleAuthService>();
        services.AddHttpContextAccessor();

        // ── Payment gateways ────────────────────────────────────────────────
        services.AddHttpClient<IPaymentGatewayService, PayOSGatewayService>((sp, client) =>
        {
            var payOsSettings = sp.GetRequiredService<IOptions<PayOSSettings>>().Value;
            client.BaseAddress = new Uri(payOsSettings.BaseUrl);
        });
        services.AddScoped<IPaymentGatewayResolver, PaymentGatewayResolver>();

        // ── AI chatbot ──────────────────────────────────────────────────────
        services.AddScoped<IAiChatService, GeminiChatService>();

        // ── Use Case Handlers ────────────────────────────────────────────────
        // Auth, Staff, Feedbacks, Posts, Services, Inventory, Medicines, Schedules,
        // Promotions, LeaveRequests, Rooms, ClinicInfo, Chat, AiAnalytics, ActivityLogs,
        // Notifications, Patients, StaffDashboard and (từ đợt này) Booking/Queue/ClinicalRecords/
        // AiAssist/Reminders/DentistDashboard/Dentists handlers are now auto-registered by
        // MediatR (implement IRequestHandler<,>). Remaining registrations below are for
        // the "god-handlers" Invoice/Payment/Dashboard — deferred to a later refactor phase.
        services.AddScoped<GetStaffHandler>();
        services.AddScoped<PatientAccessHelper>();

        // 5 handler dưới đây ĐÃ là MediatR handler nhưng vẫn cần đăng ký theo KIỂU CỤ THỂ vì đang
        // được inject trực tiếp (không qua ISender) ở nơi khác:
        //  - GetDentistsHandler, GetDentistSlotsHandler, CreateAppointmentHandler,
        //    CancelAppointmentHandler → SendChatMessageHandler
        //  - GetWaitingQueueHandler → GetPatientQueueHandler (phải dùng đúng thuật toán đánh số hàng đợi)
        services.AddScoped<GetDentistsHandler>();
        services.AddScoped<GetDentistSlotsHandler>();
        services.AddScoped<CreateAppointmentHandler>();
        services.AddScoped<CancelAppointmentHandler>();
        services.AddScoped<GetWaitingQueueHandler>();

        services.AddScoped<InvoiceQueryHelper>();
        services.AddScoped<IPaymentConfirmationService, PaymentConfirmationService>();
        services.AddScoped<DentalClinic.API.Application.UseCases.ClinicalRecords.TreatmentPlanQueryHelper>();

        return services;
    }
}
