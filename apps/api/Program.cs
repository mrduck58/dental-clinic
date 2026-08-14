using System.Net;
using System.Text;
using DentalClinic.API.Application.DependencyInjection;
using DentalClinic.API.Infrastructure.Extensions;
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
builder.Services.AddControllers();

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
    await db.Database.MigrateAsync();
    await DataSeeder.SeedAsync(db);
}
if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();
app.UseAuthentication();
app.UseMiddleware<AccountStatusMiddleware>();
app.UseAuthorization();
app.MapControllers();

// Health check endpoint cho keep-alive (cron-job.org, uptime monitor) và warm-up
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    service = "DentalClinic API",
    timestamp = DateTime.UtcNow
}));

await app.RunAsync();

