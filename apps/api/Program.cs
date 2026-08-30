using System.Net;
using System.Text;
using DentalClinic.API.Application.DependencyInjection;
using DentalClinic.API.Infrastructure.Extensions;
using DentalClinic.API.Infrastructure.Hubs;
using DentalClinic.API.Infrastructure.Persistence;
using DentalClinic.API.Presentation.Middlewares;
using DentalClinic.API.Presentation.RateLimiting;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

// System.Net cũng có kiểu IPNetwork trùng tên; ForwardedHeadersOptions.KnownNetworks dùng kiểu của
// HttpOverrides nên chỉ đích danh, tránh CS0104.
using IPNetwork = Microsoft.AspNetCore.HttpOverrides.IPNetwork;

var builder = WebApplication.CreateBuilder(args);

// ── Services ───────────────────────────────────────────────────────────────
builder.Services.AddOpenApi();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddSignalR();

// Sau reverse proxy (nginx), Connection.RemoteIpAddress là IP container nginx với MỌI request.
// Không dịch lại thì rate limiting theo IP sẽ gom cả thế giới vào một xô và tự khóa toàn hệ thống.
//
// Không sợ giả mạo qua nginx: nginx dùng $proxy_add_x_forwarded_for, tức NỐI THÊM IP thật của
// client vào cuối X-Forwarded-For. ForwardLimit = 1 lấy phần tử ngoài cùng bên phải, nên header
// do client tự bịa luôn nằm bên trái và bị bỏ qua.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = 1;

    // Mặc định ASP.NET Core chỉ tin loopback. nginx nằm trong mạng bridge của Docker nên IP của nó
    // thay đổi mỗi lần dựng lại — tin theo dải mạng riêng thay vì một địa chỉ cố định.
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
    options.KnownNetworks.Add(new IPNetwork(IPAddress.Parse("10.0.0.0"), 8));
    options.KnownNetworks.Add(new IPNetwork(IPAddress.Parse("172.16.0.0"), 12));
    options.KnownNetworks.Add(new IPNetwork(IPAddress.Parse("192.168.0.0"), 16));
    options.KnownNetworks.Add(new IPNetwork(IPAddress.Parse("127.0.0.0"), 8));
});

builder.Services.AddAuthRateLimiting();

// FluentValidation — tự động validate [FromBody] trước khi vào controller (auto-validation wiring
// là mối quan tâm riêng của tầng Web/MVC nên vẫn ở đây; đăng ký validator cụ thể nằm trong AddApplication()).
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddApplication();

// Chuẩn hóa response lỗi validation → { title, status } nhất quán với ExceptionMiddleware
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = ctx =>
    {
        var firstError = ctx.ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .FirstOrDefault() ?? "Dữ liệu không hợp lệ.";

        return new UnprocessableEntityObjectResult(new { title = firstError, status = 422 });
    };
});

// Infrastructure: DbContext, Repositories, JwtService, EmailService, Handlers
builder.Services.AddInfrastructure(builder.Configuration);

// JWT Authentication
//
// Kiểm IsNullOrWhiteSpace, KHÔNG chỉ null. appsettings.json giao sẵn "Secret": "" làm chỗ trống, nên
// khi thiếu biến môi trường thì config trả về CHUỖI RỖNG và toán tử ?? không bao giờ kích hoạt.
// Hệ quả cũ: SymmetricSecurityKey ném ArgumentNullException bên trong factory của AddJwtBearer, mà
// factory đó chỉ chạy LAZY ở request đầu tiên — Render báo deploy thành công, health check vào file
// tĩnh vẫn 200, nhưng MỌI /api/* trả 500 vì UseAuthentication chạy cho cả endpoint ẩn danh.
// Chết ngay lúc khởi động kèm tên biến còn thiếu thì dễ sửa hơn nhiều.
//
// Chặn luôn secret quá ngắn: HS256 cần khoá đủ dài, thiếu thì lỗi lại nổ muộn ở lần ký token đầu tiên
// thay vì ở đây.
const int minSecretLength = 32;
var jwtSection = builder.Configuration.GetSection("JwtSettings");

var missingJwtKeys = new[] { "Secret", "Issuer", "Audience" }
    .Where(key => string.IsNullOrWhiteSpace(jwtSection[key]))
    .Select(key => $"JwtSettings__{key}")
    .ToArray();

if (missingJwtKeys.Length > 0)
    throw new InvalidOperationException(
        $"Thiếu cấu hình JWT: {string.Join(", ", missingJwtKeys)}. " +
        "Set các biến môi trường này trước khi khởi động (trên Render: tab Environment).");

var jwtSecret = jwtSection["Secret"]!;

if (jwtSecret.Length < minSecretLength)
    throw new InvalidOperationException(
        $"JwtSettings__Secret chỉ dài {jwtSecret.Length} ký tự, cần tối thiểu {minSecretLength} cho HS256.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = builder.Configuration["JwtSettings:Issuer"],
            ValidAudience            = builder.Configuration["JwtSettings:Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ClockSkew                = TimeSpan.Zero,
        };
    });

builder.Services.AddAuthorization();

// CORS — dev: cho phép tất cả origin (Flutter web chạy port ngẫu nhiên)
//        prod: chỉ các origin được cấu hình
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
        }
        else
        {
            policy.WithOrigins(
                    builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
                        ?? ["http://localhost:3000", "http://localhost:3001"])
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
    });
});

// ── Build App ─────────────────────────────────────────────────────────────
var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

// Phải chạy TRƯỚC mọi thứ đọc IP client (rate limiter, ghi nhật ký đăng nhập) — nó dịch
// X-Forwarded-For của nginx thành Connection.RemoteIpAddress.
app.UseForwardedHeaders();

app.UseMiddleware<ExceptionMiddleware>();
app.UseCors();

// Sau UseCors để phản hồi 429 vẫn mang đủ header CORS (không thì trình duyệt nuốt mất lỗi và
// người dùng chỉ thấy "network error"), và trước phần xác thực để chặn ngay tại cửa.
app.UseRateLimiter();

app.UseStaticFiles();

// Auto-migrate + seed
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        await db.Database.ExecuteSqlRawAsync(@"
            CREATE TABLE IF NOT EXISTS ""UserDeviceTokens"" (
                ""Id"" uuid PRIMARY KEY,
                ""UserId"" uuid NOT NULL,
                ""Token"" text NOT NULL,
                ""DeviceType"" text NULL,
                ""UpdatedAt"" timestamp with time zone NOT NULL
            );
            CREATE INDEX IF NOT EXISTS ""IX_UserDeviceTokens_UserId"" ON ""UserDeviceTokens"" (""UserId"");
            CREATE INDEX IF NOT EXISTS ""IX_UserDeviceTokens_Token"" ON ""UserDeviceTokens"" (""Token"");

            CREATE TABLE IF NOT EXISTS ""AppointmentSlotHolds"" (
                ""Id"" uuid PRIMARY KEY,
                ""PatientId"" uuid NOT NULL,
                ""UserId"" uuid NOT NULL,
                ""DentistId"" uuid NOT NULL,
                ""AppointmentDate"" timestamp with time zone NOT NULL,
                ""TimeSlot"" text NOT NULL,
                ""CreatedAt"" timestamp with time zone NOT NULL,
                ""ExpiresAt"" timestamp with time zone NOT NULL,
                ""Status"" text NOT NULL,
                ""IsSuccess"" boolean NOT NULL DEFAULT false,
                ""ServiceId"" uuid NULL,
                ""DurationMinutes"" integer NOT NULL DEFAULT 30
            );
            ALTER TABLE ""AppointmentSlotHolds"" ADD COLUMN IF NOT EXISTS ""ServiceId"" uuid NULL;
            ALTER TABLE ""AppointmentSlotHolds"" ADD COLUMN IF NOT EXISTS ""DurationMinutes"" integer NOT NULL DEFAULT 30;
            CREATE INDEX IF NOT EXISTS ""IX_AppointmentSlotHolds_Dentist_Date"" ON ""AppointmentSlotHolds"" (""DentistId"", ""AppointmentDate"");
            CREATE INDEX IF NOT EXISTS ""IX_AppointmentSlotHolds_Patient_Date"" ON ""AppointmentSlotHolds"" (""PatientId"", ""CreatedAt"");

            CREATE TABLE IF NOT EXISTS ""AppointmentChangeRequests"" (
                ""Id"" uuid PRIMARY KEY,
                ""AppointmentId"" uuid NOT NULL,
                ""PatientId"" uuid NOT NULL,
                ""RequestedByUserId"" uuid NOT NULL,
                ""Type"" character varying(20) NOT NULL,
                ""Status"" character varying(20) NOT NULL,
                ""Reason"" character varying(500) NOT NULL,
                ""DesiredDate"" timestamp with time zone NULL,
                ""DesiredTimeSlot"" character varying(50) NULL,
                ""DesiredDentistId"" uuid NULL,
                ""StaffNote"" character varying(500) NULL,
                ""ProcessedByUserId"" uuid NULL,
                ""ProcessedAt"" timestamp with time zone NULL,
                ""CreatedAt"" timestamp with time zone NOT NULL
            );
            CREATE INDEX IF NOT EXISTS ""IX_AppointmentChangeRequests_Status"" ON ""AppointmentChangeRequests"" (""Status"");
            CREATE INDEX IF NOT EXISTS ""IX_AppointmentChangeRequests_AppointmentId"" ON ""AppointmentChangeRequests"" (""AppointmentId"");
            CREATE INDEX IF NOT EXISTS ""IX_AppointmentChangeRequests_PatientId"" ON ""AppointmentChangeRequests"" (""PatientId"");
            CREATE INDEX IF NOT EXISTS ""IX_AppointmentChangeRequests_RequestedByUserId"" ON ""AppointmentChangeRequests"" (""RequestedByUserId"");

            ALTER TABLE ""Appointments"" ADD COLUMN IF NOT EXISTS ""AppointmentType"" character varying(50) NOT NULL DEFAULT 'GeneralExam';
            ALTER TABLE ""Appointments"" ADD COLUMN IF NOT EXISTS ""DurationMinutes"" integer NOT NULL DEFAULT 30;
            ALTER TABLE ""Appointments"" ADD COLUMN IF NOT EXISTS ""FollowUpId"" uuid NULL;
            ALTER TABLE ""Appointments"" ADD COLUMN IF NOT EXISTS ""FollowUpFromAppointmentId"" uuid NULL;
            ALTER TABLE ""Appointments"" ADD COLUMN IF NOT EXISTS ""FollowUpDate"" timestamp with time zone NULL;
            ALTER TABLE ""Appointments"" ADD COLUMN IF NOT EXISTS ""FollowUpNote"" text NULL;
            ALTER TABLE ""Appointments"" ADD COLUMN IF NOT EXISTS ""AiSummary"" text NULL;
            ALTER TABLE ""Appointments"" ADD COLUMN IF NOT EXISTS ""AiSummaryGeneratedAt"" timestamp with time zone NULL;
            ALTER TABLE ""Appointments"" ADD COLUMN IF NOT EXISTS ""AiSummaryBasedOnCount"" integer NULL;
            ALTER TABLE ""Appointments"" ADD COLUMN IF NOT EXISTS ""QueueEntryOrder"" bigint NULL;
            ALTER TABLE ""Appointments"" ADD COLUMN IF NOT EXISTS ""QueueOrder"" bigint NULL;
            ALTER TABLE ""Appointments"" ADD COLUMN IF NOT EXISTS ""CheckedInAt"" timestamp with time zone NULL;
            ALTER TABLE ""Appointments"" ADD COLUMN IF NOT EXISTS ""CancellationReason"" character varying(50) NULL;
            ALTER TABLE ""Appointments"" ADD COLUMN IF NOT EXISTS ""CancellationNote"" character varying(500) NULL;
            ALTER TABLE ""Appointments"" ADD COLUMN IF NOT EXISTS ""RescheduledCount"" integer NOT NULL DEFAULT 0;
            ALTER TABLE ""Appointments"" ADD COLUMN IF NOT EXISTS ""LastRescheduledAt"" timestamp with time zone NULL;
            ALTER TABLE ""Appointments"" ADD COLUMN IF NOT EXISTS ""CancelledAt"" timestamp with time zone NULL;
            ALTER TABLE ""Appointments"" ADD COLUMN IF NOT EXISTS ""CancelledByUserId"" uuid NULL;
            ALTER TABLE ""Appointments"" ADD COLUMN IF NOT EXISTS ""Origin"" character varying(20) NOT NULL DEFAULT 'Online';

            ALTER TABLE ""Services"" ADD COLUMN IF NOT EXISTS ""EstimatedSessionCount"" integer NULL;
            ALTER TABLE ""Services"" ADD COLUMN IF NOT EXISTS ""EstimatedDurationMin"" integer NULL;
            ALTER TABLE ""Services"" ADD COLUMN IF NOT EXISTS ""EstimatedDurationMax"" integer NULL;
            ALTER TABLE ""Services"" ADD COLUMN IF NOT EXISTS ""EstimatedDurationUnit"" character varying(20) NULL;

            CREATE TABLE IF NOT EXISTS ""Diagnoses"" (
                ""Id"" uuid PRIMARY KEY,
                ""AppointmentId"" uuid NOT NULL,
                ""Description"" character varying(1000) NOT NULL,
                ""MedicalHistory"" character varying(2000) NULL,
                ""AllergyHistory"" character varying(2000) NULL,
                ""Conclusion"" character varying(2000) NULL,
                ""GumCondition"" character varying(500) NULL,
                ""OralMucosaCondition"" character varying(500) NULL,
                ""GumBleeding"" character varying(500) NULL,
                ""PainOnChewing"" character varying(500) NULL,
                ""TeethCount"" character varying(500) NULL,
                ""DecayedTeeth"" character varying(500) NULL,
                ""WornOrBrokenTeeth"" character varying(500) NULL,
                ""LooseTeeth"" character varying(500) NULL,
                ""Tartar"" character varying(500) NULL,
                ""Plaque"" character varying(500) NULL,
                ""BadBreath"" character varying(500) NULL,
                ""TmjSymptoms"" character varying(500) NULL,
                ""Occlusion"" character varying(500) NULL,
                ""OcclusionDeviation"" character varying(500) NULL,
                ""CreatedAt"" timestamp with time zone NOT NULL
            );
            CREATE INDEX IF NOT EXISTS ""IX_Diagnoses_AppointmentId"" ON ""Diagnoses"" (""AppointmentId"");

            CREATE TABLE IF NOT EXISTS ""TreatmentPlans"" (
                ""Id"" uuid PRIMARY KEY,
                ""PatientId"" uuid NOT NULL,
                ""DentistId"" uuid NOT NULL,
                ""AppointmentId"" uuid NULL,
                ""Title"" character varying(200) NOT NULL DEFAULT 'Kế hoạch điều trị',
                ""Status"" character varying(50) NOT NULL DEFAULT 'Planned',
                ""Notes"" character varying(2000) NULL,
                ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT timezone('utc'::text, now()),
                ""CompletedAt"" timestamp with time zone NULL
            );
            ALTER TABLE ""TreatmentPlans"" ADD COLUMN IF NOT EXISTS ""Title"" character varying(200) NOT NULL DEFAULT 'Kế hoạch điều trị';
            ALTER TABLE ""TreatmentPlans"" ADD COLUMN IF NOT EXISTS ""Status"" character varying(50) NOT NULL DEFAULT 'Planned';
            ALTER TABLE ""TreatmentPlans"" ADD COLUMN IF NOT EXISTS ""Notes"" character varying(2000) NULL;
            ALTER TABLE ""TreatmentPlans"" ADD COLUMN IF NOT EXISTS ""AppointmentId"" uuid NULL;
            ALTER TABLE ""TreatmentPlans"" ADD COLUMN IF NOT EXISTS ""CompletedAt"" timestamp with time zone NULL;
            ALTER TABLE ""TreatmentPlans"" DROP CONSTRAINT IF EXISTS ""FK_TreatmentPlans_Services_ServiceId"";
            DO $$ BEGIN
                IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'TreatmentPlans' AND column_name = 'ServiceId') THEN
                    ALTER TABLE ""TreatmentPlans"" ALTER COLUMN ""ServiceId"" DROP NOT NULL;
                    ALTER TABLE ""TreatmentPlans"" ALTER COLUMN ""ServiceId"" DROP DEFAULT;
                END IF;
                IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'TreatmentPlans' AND column_name = 'UnitPrice') THEN
                    ALTER TABLE ""TreatmentPlans"" ALTER COLUMN ""UnitPrice"" DROP NOT NULL;
                    ALTER TABLE ""TreatmentPlans"" ALTER COLUMN ""UnitPrice"" DROP DEFAULT;
                END IF;
                IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'TreatmentPlans' AND column_name = 'Quantity') THEN
                    ALTER TABLE ""TreatmentPlans"" ALTER COLUMN ""Quantity"" DROP NOT NULL;
                    ALTER TABLE ""TreatmentPlans"" ALTER COLUMN ""Quantity"" DROP DEFAULT;
                END IF;
            END $$;
            CREATE INDEX IF NOT EXISTS ""IX_TreatmentPlans_PatientId"" ON ""TreatmentPlans"" (""PatientId"");
            CREATE INDEX IF NOT EXISTS ""IX_TreatmentPlans_AppointmentId"" ON ""TreatmentPlans"" (""AppointmentId"");

            CREATE TABLE IF NOT EXISTS ""TreatmentPlanItems"" (
                ""Id"" uuid PRIMARY KEY,
                ""TreatmentPlanId"" uuid NOT NULL,
                ""ServiceId"" uuid NOT NULL,
                ""ServiceOptionId"" uuid NULL,
                ""ServiceOptionName"" character varying(200) NULL,
                ""UnitPrice"" numeric(18, 2) NOT NULL,
                ""Quantity"" integer NOT NULL,
                ""Teeth"" character varying(200) NULL,
                ""Status"" character varying(50) NOT NULL,
                ""WarrantyUntil"" date NULL,
                ""Notes"" character varying(2000) NULL,
                ""EstimatedSessionCount"" integer NULL,
                ""EstimatedDurationMin"" integer NULL,
                ""EstimatedDurationMax"" integer NULL,
                ""EstimatedDurationUnit"" character varying(20) NULL,
                ""EstimatedStartDate"" date NULL,
                ""EstimatedEndDate"" date NULL,
                ""CreatedAt"" timestamp with time zone NOT NULL,
                ""CompletedAt"" timestamp with time zone NULL
            );
            ALTER TABLE ""TreatmentPlanItems"" ADD COLUMN IF NOT EXISTS ""EstimatedSessionCount"" integer NULL;
            ALTER TABLE ""TreatmentPlanItems"" ADD COLUMN IF NOT EXISTS ""EstimatedDurationMin"" integer NULL;
            ALTER TABLE ""TreatmentPlanItems"" ADD COLUMN IF NOT EXISTS ""EstimatedDurationMax"" integer NULL;
            ALTER TABLE ""TreatmentPlanItems"" ADD COLUMN IF NOT EXISTS ""EstimatedDurationUnit"" character varying(20) NULL;
            ALTER TABLE ""TreatmentPlanItems"" ADD COLUMN IF NOT EXISTS ""EstimatedStartDate"" date NULL;
            ALTER TABLE ""TreatmentPlanItems"" ADD COLUMN IF NOT EXISTS ""EstimatedEndDate"" date NULL;
            CREATE INDEX IF NOT EXISTS ""IX_TreatmentPlanItems_TreatmentPlanId"" ON ""TreatmentPlanItems"" (""TreatmentPlanId"");
            CREATE INDEX IF NOT EXISTS ""IX_TreatmentPlanItems_ServiceId"" ON ""TreatmentPlanItems"" (""ServiceId"");

            CREATE TABLE IF NOT EXISTS ""TreatmentProcedures"" (
                ""Id"" uuid PRIMARY KEY,
                ""ServiceId"" uuid NOT NULL,
                ""StepNumber"" integer NOT NULL,
                ""Name"" character varying(300) NOT NULL,
                ""EstimatedMinutes"" integer NOT NULL DEFAULT 30,
                ""Description"" text NULL
            );
            CREATE INDEX IF NOT EXISTS ""IX_TreatmentProcedures_ServiceId"" ON ""TreatmentProcedures"" (""ServiceId"");

            CREATE TABLE IF NOT EXISTS ""TreatmentSessions"" (
                ""Id"" uuid PRIMARY KEY,
                ""TreatmentPlanItemId"" uuid NOT NULL,
                ""TreatmentProcedureId"" uuid NULL,
                ""DentistId"" uuid NULL,
                ""SessionNumber"" integer NOT NULL DEFAULT 1,
                ""Name"" character varying(200) NOT NULL DEFAULT '',
                ""Status"" character varying(50) NOT NULL DEFAULT 'Planned',
                ""DurationMinutes"" integer NOT NULL DEFAULT 30,
                ""PerformedAt"" timestamp with time zone NULL,
                ""Note"" character varying(2000) NULL,
                ""Percent"" integer NOT NULL DEFAULT 0,
                ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT timezone('utc'::text, now())
            );
            ALTER TABLE ""TreatmentSessions"" ADD COLUMN IF NOT EXISTS ""DurationMinutes"" integer NOT NULL DEFAULT 30;
            ALTER TABLE ""TreatmentSessions"" ADD COLUMN IF NOT EXISTS ""SessionNumber"" integer NOT NULL DEFAULT 1;
            ALTER TABLE ""TreatmentSessions"" ADD COLUMN IF NOT EXISTS ""Name"" character varying(200) NOT NULL DEFAULT '';
            ALTER TABLE ""TreatmentSessions"" ADD COLUMN IF NOT EXISTS ""Status"" character varying(50) NOT NULL DEFAULT 'Planned';
            ALTER TABLE ""TreatmentSessions"" ADD COLUMN IF NOT EXISTS ""Percent"" integer NOT NULL DEFAULT 0;
            ALTER TABLE ""TreatmentSessions"" ADD COLUMN IF NOT EXISTS ""DentistId"" uuid NULL;
            ALTER TABLE ""TreatmentSessions"" ADD COLUMN IF NOT EXISTS ""PerformedAt"" timestamp with time zone NULL;
            ALTER TABLE ""TreatmentSessions"" ADD COLUMN IF NOT EXISTS ""TreatmentProcedureId"" uuid NULL;
            CREATE INDEX IF NOT EXISTS ""IX_TreatmentSessions_TreatmentPlanItemId"" ON ""TreatmentSessions"" (""TreatmentPlanItemId"");

            CREATE TABLE IF NOT EXISTS ""StepProgressEntries"" (
                ""Id"" uuid PRIMARY KEY,
                ""TreatmentSessionId"" uuid NOT NULL,
                ""CompletionPercentage"" integer NOT NULL DEFAULT 0,
                ""Note"" character varying(1000) NULL,
                ""RecordedAt"" timestamp with time zone NOT NULL DEFAULT timezone('utc'::text, now()),
                ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT timezone('utc'::text, now())
            );
            ALTER TABLE ""StepProgressEntries"" ADD COLUMN IF NOT EXISTS ""TreatmentSessionId"" uuid NULL;
            CREATE INDEX IF NOT EXISTS ""IX_StepProgressEntries_TreatmentSessionId"" ON ""StepProgressEntries"" (""TreatmentSessionId"");

            CREATE TABLE IF NOT EXISTS ""AppointmentSessions"" (
                ""Id"" uuid PRIMARY KEY,
                ""AppointmentId"" uuid NOT NULL,
                ""TreatmentSessionId"" uuid NOT NULL,
                ""Sequence"" integer NOT NULL DEFAULT 1,
                ""DurationMinutes"" integer NOT NULL DEFAULT 30,
                ""Note"" character varying(1000) NULL,
                ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT timezone('utc'::text, now())
            );
            ALTER TABLE ""AppointmentSessions"" ADD COLUMN IF NOT EXISTS ""DurationMinutes"" integer NOT NULL DEFAULT 30;
            ALTER TABLE ""AppointmentSessions"" ADD COLUMN IF NOT EXISTS ""Sequence"" integer NOT NULL DEFAULT 1;
            ALTER TABLE ""AppointmentSessions"" ADD COLUMN IF NOT EXISTS ""Note"" character varying(1000) NULL;
            CREATE INDEX IF NOT EXISTS ""IX_AppointmentSessions_AppointmentId"" ON ""AppointmentSessions"" (""AppointmentId"");
            CREATE INDEX IF NOT EXISTS ""IX_AppointmentSessions_TreatmentSessionId"" ON ""AppointmentSessions"" (""TreatmentSessionId"");

            CREATE TABLE IF NOT EXISTS ""FollowUps"" (
                ""Id"" uuid PRIMARY KEY,
                ""PatientId"" uuid NOT NULL,
                ""DentistId"" uuid NOT NULL,
                ""OriginAppointmentId"" uuid NOT NULL,
                ""TreatmentPlanItemId"" uuid NULL,
                ""TreatmentSessionId"" uuid NULL,
                ""AppointmentId"" uuid NULL,
                ""DueDate"" timestamp with time zone NOT NULL,
                ""Note"" character varying(2000) NULL,
                ""Status"" character varying(50) NOT NULL,
                ""CreatedAt"" timestamp with time zone NOT NULL,
                ""CompletedAt"" timestamp with time zone NULL,
                ""CancelledAt"" timestamp with time zone NULL
            );
            CREATE INDEX IF NOT EXISTS ""IX_FollowUps_PatientId"" ON ""FollowUps"" (""PatientId"");
            CREATE INDEX IF NOT EXISTS ""IX_FollowUps_DueDate"" ON ""FollowUps"" (""DueDate"");
            CREATE INDEX IF NOT EXISTS ""IX_FollowUps_Status"" ON ""FollowUps"" (""Status"");

            CREATE TABLE IF NOT EXISTS ""Prescriptions"" (
                ""Id"" uuid PRIMARY KEY,
                ""AppointmentId"" uuid NOT NULL,
                ""CreatedAt"" timestamp with time zone NOT NULL,
                ""Notes"" character varying(2000) NULL
            );
            CREATE INDEX IF NOT EXISTS ""IX_Prescriptions_AppointmentId"" ON ""Prescriptions"" (""AppointmentId"");

            CREATE TABLE IF NOT EXISTS ""PrescriptionItems"" (
                ""Id"" uuid PRIMARY KEY,
                ""PrescriptionId"" uuid NOT NULL,
                ""MedicineName"" character varying(200) NOT NULL,
                ""Dosage"" character varying(50) NOT NULL,
                ""Quantity"" integer NOT NULL,
                ""Unit"" character varying(20) NOT NULL,
                ""Usage"" character varying(500) NOT NULL,
                ""Notes"" character varying(500) NULL
            );
            CREATE INDEX IF NOT EXISTS ""IX_PrescriptionItems_PrescriptionId"" ON ""PrescriptionItems"" (""PrescriptionId"");

            CREATE TABLE IF NOT EXISTS ""AppointmentPhotos"" (
                ""Id"" uuid PRIMARY KEY,
                ""AppointmentId"" uuid NOT NULL,
                ""Section"" character varying(30) NOT NULL,
                ""Url"" character varying(1000) NOT NULL,
                ""Note"" character varying(1000) NULL,
                ""UploadedBy"" character varying(200) NOT NULL,
                ""CreatedAt"" timestamp with time zone NOT NULL
            );
            CREATE INDEX IF NOT EXISTS ""IX_AppointmentPhotos_AppointmentId_Section"" ON ""AppointmentPhotos"" (""AppointmentId"", ""Section"");

            CREATE TABLE IF NOT EXISTS ""TreatmentSupplyUsages"" (
                ""Id"" uuid PRIMARY KEY,
                ""TreatmentPlanId"" uuid NULL,
                ""TreatmentSessionId"" uuid NULL,
                ""SupplyItemId"" uuid NOT NULL,
                ""SupplyTransactionId"" uuid NULL,
                ""StepEntryId"" uuid NULL,
                ""Quantity"" integer NOT NULL,
                ""UnitCostAtUsage"" numeric(18, 2) NOT NULL DEFAULT 0,
                ""CreatedBy"" character varying(200) NULL,
                ""CreatedAt"" timestamp with time zone NOT NULL
            );
            ALTER TABLE ""TreatmentSupplyUsages"" ADD COLUMN IF NOT EXISTS ""TreatmentSessionId"" uuid NULL;
            CREATE INDEX IF NOT EXISTS ""IX_TreatmentSupplyUsages_TreatmentPlanId"" ON ""TreatmentSupplyUsages"" (""TreatmentPlanId"");
            CREATE INDEX IF NOT EXISTS ""IX_TreatmentSupplyUsages_TreatmentSessionId"" ON ""TreatmentSupplyUsages"" (""TreatmentSessionId"");

            ALTER TABLE ""InvoiceItems"" ADD COLUMN IF NOT EXISTS ""TreatmentPlanItemId"" uuid NULL;
            ALTER TABLE ""InvoiceItems"" ADD COLUMN IF NOT EXISTS ""TreatmentPlanId"" uuid NULL;
            ALTER TABLE ""InvoiceItems"" ADD COLUMN IF NOT EXISTS ""AmountCollected"" numeric(18, 2) NOT NULL DEFAULT 0;
            CREATE INDEX IF NOT EXISTS ""IX_InvoiceItems_TreatmentPlanItemId"" ON ""InvoiceItems"" (""TreatmentPlanItemId"");
            CREATE INDEX IF NOT EXISTS ""IX_InvoiceItems_TreatmentPlanId"" ON ""InvoiceItems"" (""TreatmentPlanId"");

            ALTER TABLE ""Invoices"" ADD COLUMN IF NOT EXISTS ""PatientId"" uuid NULL;
            ALTER TABLE ""Invoices"" ADD COLUMN IF NOT EXISTS ""ParentInvoiceId"" uuid NULL;
            ALTER TABLE ""Invoices"" ADD COLUMN IF NOT EXISTS ""IsSettled"" boolean NOT NULL DEFAULT false;
            ALTER TABLE ""Invoices"" ADD COLUMN IF NOT EXISTS ""CollectingRemaining"" boolean NOT NULL DEFAULT false;
            ALTER TABLE ""Invoices"" ADD COLUMN IF NOT EXISTS ""TreatmentPlanId"" uuid NULL;
            ALTER TABLE ""Invoices"" ADD COLUMN IF NOT EXISTS ""PromotionId"" uuid NULL;
            ALTER TABLE ""Invoices"" ALTER COLUMN ""AppointmentId"" DROP NOT NULL;
        ");
        await db.Database.MigrateAsync();
        await DataSeeder.SeedAsync(db);
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogWarning(ex, "Lưu ý: Tự động migrate/seed database bị bỏ qua (do dùng Supabase Pooler hoặc đã cập nhật trước đó). API tiếp tục khởi động.");
    }
}
if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();
app.UseAuthentication();
app.UseMiddleware<AccountStatusMiddleware>();
app.UseAuthorization();
app.MapControllers();
app.MapHub<BookingHub>("/hubs/booking");

// Health check endpoint cho keep-alive (cron-job.org, uptime monitor) và warm-up
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    service = "DentalClinic API",
    timestamp = DateTime.UtcNow
}));

await app.RunAsync();

