using DentalClinic.API.Application.UseCases.ActivityLogs;
using DentalClinic.API.Application.UseCases.AiAnalytics;
using DentalClinic.API.Application.UseCases.Chat;
using DentalClinic.API.Application.UseCases.Dashboard;
using DentalClinic.API.Application.UseCases.StaffDashboard;
using DentalClinic.API.Application.UseCases.Notifications;
using DentalClinic.API.Application.UseCases.Appointments;
using DentalClinic.API.Application.UseCases.Inventory;
using DentalClinic.API.Application.UseCases.Auth;
using DentalClinic.API.Application.UseCases.ClinicInfo;
using DentalClinic.API.Application.UseCases.Invoices;
using DentalClinic.API.Application.UseCases.Payments;
using DentalClinic.API.Application.UseCases.Medicines;
using DentalClinic.API.Application.UseCases.Feedbacks;
using DentalClinic.API.Application.UseCases.LeaveRequests;
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

        // ── Settings ────────────────────────────────────────────────────────
        services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));
        services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));
        services.Configure<PayOSSettings>(configuration.GetSection("PayOSSettings"));
        services.Configure<GeminiSettings>(configuration.GetSection("GeminiSettings"));
        services.Configure<GoogleAuthSettings>(configuration.GetSection("GoogleAuthSettings"));

        // ── Repositories ────────────────────────────────────────────────────
        services.AddScoped<IAppointmentRepository, AppointmentRepository>();
        services.AddScoped<IPatientRepository, PatientRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
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
        services.AddScoped<LoginHandler>();
        services.AddScoped<RegisterHandler>();
        services.AddScoped<VerifyOtpHandler>();
        services.AddScoped<ResendOtpHandler>();
        services.AddScoped<FillProfileHandler>();
        services.AddScoped<GetMyProfileHandler>();
        services.AddScoped<ChangePasswordHandler>();
        services.AddScoped<CreateAccountHandler>();
        services.AddScoped<GetAccountsHandler>();
        services.AddScoped<ForgotPasswordHandler>();
        services.AddScoped<ResetPasswordHandler>();
        services.AddScoped<GoogleLoginHandler>();
        services.AddScoped<ForgotPasswordOtpHandler>();
        services.AddScoped<VerifyPasswordResetOtpHandler>();

        services.AddScoped<GetStaffHandler>();
        services.AddScoped<GetDentistsHandler>();
        services.AddScoped<GetDentistDetailHandler>();
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
        services.AddScoped<GenerateFeedbackReplyHandler>();

        services.AddScoped<GetPostsHandler>();
        services.AddScoped<GetPostByIdHandler>();
        services.AddScoped<CreatePostHandler>();
        services.AddScoped<UpdatePostHandler>();
        services.AddScoped<DeletePostHandler>();
        services.AddScoped<GenerateMarketingContentHandler>();

        services.AddScoped<GetServicesHandler>();
        services.AddScoped<GetServiceByIdHandler>();
        services.AddScoped<CreateServiceHandler>();
        services.AddScoped<UpdateServiceHandler>();
        services.AddScoped<DeleteServiceHandler>();
        services.AddScoped<ToggleServiceStatusHandler>();

        services.AddScoped<GetSupplyItemsHandler>();
        services.AddScoped<GetSupplyTransactionsHandler>();
        services.AddScoped<CreateSupplyItemHandler>();
        services.AddScoped<CreateSupplyTransactionHandler>();
        services.AddScoped<GetMaterialRequestsHandler>();
        services.AddScoped<MarkMaterialRequestDoneHandler>();
        services.AddScoped<StockImportHandler>();

        services.AddScoped<GetMedicinesHandler>();
        services.AddScoped<GetMedicineByIdHandler>();
        services.AddScoped<CreateMedicineHandler>();
        services.AddScoped<UpdateMedicineHandler>();
        services.AddScoped<DeleteMedicineHandler>();

        services.AddScoped<GetWeekScheduleHandler>();
        services.AddScoped<SaveWeekScheduleHandler>();
        services.AddScoped<GetMyScheduleHandler>();

        services.AddScoped<GetPromotionsHandler>();
        services.AddScoped<GetPromotionByIdHandler>();
        services.AddScoped<CreatePromotionHandler>();
        services.AddScoped<UpdatePromotionHandler>();
        services.AddScoped<DeletePromotionHandler>();
        services.AddScoped<TogglePromotionStatusHandler>();

        services.AddScoped<GetLeaveRequestsHandler>();
        services.AddScoped<GetMyLeaveRequestsHandler>();
        services.AddScoped<GetLeaveRequestByIdHandler>();
        services.AddScoped<CreateLeaveRequestHandler>();
        services.AddScoped<ApproveLeaveRequestHandler>();
        services.AddScoped<RejectLeaveRequestHandler>();
        services.AddScoped<CancelLeaveRequestHandler>();
      
        services.AddScoped<GetDentistSlotsHandler>();
        services.AddScoped<CreateAppointmentHandler>();
        services.AddScoped<GetMyAppointmentsHandler>();
        services.AddScoped<GetAllAppointmentsHandler>();
        services.AddScoped<GetWaitingQueueHandler>();
        services.AddScoped<GetPatientQueueHandler>();
        services.AddScoped<TransferQueuePatientHandler>();
        services.AddScoped<ReorderQueuePatientHandler>();
        services.AddScoped<GetDentistPatientsHandler>();
        services.AddScoped<DentistDashboardHandler>();
        services.AddScoped<UpdateAppointmentStatusHandler>();
        services.AddScoped<GetExaminationHandler>();
        services.AddScoped<DiagnosisHandler>();
        services.AddScoped<TreatmentPlanHandler>();
        services.AddScoped<TreatmentProcedureHandler>();
        services.AddScoped<PrescriptionHandler>();
        services.AddScoped<GetMedicationRemindersHandler>();
        services.AddScoped<DentistReviewHandler>();
        services.AddScoped<FollowUpReminderHandler>();
        services.AddScoped<GetStaffScheduleHandler>();
        services.AddScoped<CreateWalkInAppointmentHandler>();
        services.AddScoped<SummarizePatientHistoryHandler>();
        services.AddScoped<SuggestTreatmentHandler>();
        services.AddScoped<SuggestPrescriptionHandler>();

        services.AddScoped<GetRoomsHandler>();
        services.AddScoped<GetRoomByIdHandler>();
        services.AddScoped<CreateRoomHandler>();
        services.AddScoped<UpdateRoomHandler>();
        services.AddScoped<DeleteRoomHandler>();
        services.AddScoped<ChangeRoomStatusHandler>();

        services.AddScoped<GetClinicInfoHandler>();
        services.AddScoped<UpdateClinicInfoHandler>();

        services.AddScoped<StartConversationHandler>();
        services.AddScoped<SendChatMessageHandler>();
        services.AddScoped<GetMyConversationsHandler>();
        services.AddScoped<GetConversationMessagesHandler>();
        services.AddScoped<GetAiAnalyticsHandler>();

        services.AddScoped<GetActivityLogsHandler>();
        services.AddScoped<GetNotificationsHandler>();
        services.AddScoped<InvoiceHandler>();
        services.AddScoped<PaymentHandler>();
        services.AddScoped<DashboardHandler>();
        services.AddScoped<StaffDashboardHandler>();

        return services;
    }
}
