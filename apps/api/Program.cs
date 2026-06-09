using DentalClinic.API.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

// ── Services ───────────────────────────────────────────────────────────────
builder.Services.AddOpenApi();
builder.Services.AddControllers();

// Đăng ký toàn bộ service từ tầng Infrastructure (DbContext, Repositories, v.v.)
builder.Services.AddInfrastructure(builder.Configuration);

// TODO: Thêm MediatR, FluentValidation, JWT Authentication, CORS...
// builder.Services.AddApplicationServices();
// builder.Services.AddJwtAuthentication(builder.Configuration);

// ── Build App ─────────────────────────────────────────────────────────────
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
