using System.Text.Json;
using DentalClinic.API.Domain.Exceptions;

namespace DentalClinic.API.Presentation.Middlewares;

public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await next(ctx);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            await WriteErrorAsync(ctx, ex);
        }
    }

    private static Task WriteErrorAsync(HttpContext ctx, Exception ex)
    {
        var (status, title) = ex switch
        {
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized,           ex.Message),
            ConflictException           => (StatusCodes.Status409Conflict,               ex.Message),
            NotFoundException           => (StatusCodes.Status404NotFound,               ex.Message),
            ValidationException         => (StatusCodes.Status422UnprocessableEntity,    ex.Message),
            _                           => (StatusCodes.Status500InternalServerError,    "Đã xảy ra lỗi hệ thống.")
        };

        ctx.Response.StatusCode  = status;
        ctx.Response.ContentType = "application/json";

        var body = JsonSerializer.Serialize(new { title, status });
        return ctx.Response.WriteAsync(body);
    }
}
