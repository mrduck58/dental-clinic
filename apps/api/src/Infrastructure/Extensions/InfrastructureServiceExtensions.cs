using DentalClinic.API.Application.UseCases.Invoices;
using DentalClinic.API.Application.UseCases.Patients;
using DentalClinic.API.Application.UseCases.Payments;
using DentalClinic.API.Application.Interfaces;
using DentalClinic.API.Application.UseCases.Staff;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using DentalClinic.API.Infrastructure.Persistence;
using DentalClinic.API.Infrastructure.Persistence.Repositories;
using DentalClinic.API.Infrastructure.Services;
using DentalClinic.API.Infrastructure.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
        services.Configure<SmsSettings>(configuration.GetSection("SmsSettings"));
        services.Configure<SupabaseStorageSettings>(configuration.GetSection("SupabaseStorage"));

        // ── Repositories ────────────────────────────────────────────────────
        services.AddScoped<IAppointmentRepository, AppointmentRepository>();
        services.AddScoped<IPatientRepository, PatientRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
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
        services.AddScoped<IPayrollRepository, PayrollRepository>();
        services.AddScoped<IExpenseRepository, ExpenseRepository>();
        services.AddScoped<ICommissionRuleRepository, CommissionRuleRepository>();
        services.AddScoped<IAppointmentSummaryReader, AppointmentSummaryReader>();
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<IPaymentTransactionRepository, PaymentTransactionRepository>();
        // AppDbContext đã implement IUnitOfWork — resolve về ĐÚNG instance scoped của request này
        // (không tạo DbContext mới) để các repository stage thay đổi và unit of work chốt lưu chung 1 lần.
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());

        // ── Query-service (đọc tổng hợp đa entity — không phải repository CRUD) ────
        services.AddScoped<IDashboardQueryService, DashboardQueryService>();
        services.AddScoped<IStaffDashboardQueryService, StaffDashboardQueryService>();
        services.AddScoped<IDentistDashboardQueryService, DentistDashboardQueryService>();
        services.AddScoped<IOwnerDashboardQueryService, OwnerDashboardQueryService>();
        services.AddScoped<IRevenueQueryService, RevenueQueryService>();
        services.AddScoped<IExpenseQueryService, ExpenseQueryService>();
        services.AddScoped<ICommissionQueryService, CommissionQueryService>();
        services.AddScoped<IFinanceOverviewQueryService, FinanceOverviewQueryService>();

        // ── Services ────────────────────────────────────────────────────────
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IActivityLogService, ActivityLogService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddHttpClient<IEmailService, EmailService>();
        // Kho lưu file: có cấu hình Supabase thì dùng, không thì lùi về đĩa cục bộ.
        // Đĩa cục bộ KHÔNG dùng được khi deploy lên Render — filesystem ở đó là ephemeral, file bị
        // xóa sau mỗi lần deploy/restart/spin-down. Chỉ giữ nhánh này cho máy dev.
        var storageSettings = configuration.GetSection("SupabaseStorage").Get<SupabaseStorageSettings>()
            ?? new SupabaseStorageSettings();

        if (storageSettings.IsConfigured)
            services.AddHttpClient<IFileStorageService, SupabaseFileStorageService>();
        else
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

        // ── SMS ─────────────────────────────────────────────────────────────
        services.AddHttpClient<ISmsService, SpeedSmsService>((sp, client) =>
        {
            var smsSettings = sp.GetRequiredService<IOptions<SmsSettings>>().Value;
            client.BaseAddress = new Uri(smsSettings.BaseUrl);
        });

        // ── AI chatbot ──────────────────────────────────────────────────────
        services.AddScoped<IAiChatService, GeminiChatService>();

        // ── Use Case Handlers ────────────────────────────────────────────────
        // Toàn bộ MediatR handler (implement IRequestHandler<,>) được AddApplication() tự quét và
        // đăng ký — Controller/handler khác chỉ cần ISender, không còn chỗ nào tiêm handler cụ thể.
        // Các class dưới đây KHÔNG phải MediatR handler (chỉ là helper thường được handler khác tiêm
        // trực tiếp qua constructor) nên vẫn cần đăng ký kiểu cụ thể riêng.
        services.AddScoped<GetStaffHandler>();
        services.AddScoped<PatientAccessHelper>();
        services.AddScoped<InvoiceQueryHelper>();
        services.AddScoped<IPaymentConfirmationService, PaymentConfirmationService>();
        services.AddScoped<DentalClinic.API.Application.UseCases.ClinicalRecords.TreatmentPlanQueryHelper>();
        services.AddScoped<DentalClinic.API.Application.UseCases.ClinicalRecords.ClinicalRecordWriteGuard>();
        services.AddScoped<DentalClinic.API.Application.UseCases.Booking.AppointmentChangeGuard>();
        services.AddScoped<DentalClinic.API.Application.UseCases.Booking.AppointmentSlotGuard>();

        return services;
    }
}
