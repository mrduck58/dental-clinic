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
var jwtSecret = builder.Configuration["JwtSettings:Secret"]
    ?? throw new InvalidOperationException("JwtSettings:Secret is not configured.");

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

await app.RunAsync();
