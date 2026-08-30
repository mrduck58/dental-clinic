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
        var detail = ex is Microsoft.EntityFrameworkCore.DbUpdateException dbEx && dbEx.InnerException != null
            ? $"{dbEx.Message} -> {dbEx.InnerException.Message}"
            : ex.Message;

        var (status, title) = ex switch
        {
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized,           detail),
            ForbiddenException          => (StatusCodes.Status403Forbidden,              detail),
            ConflictException           => (StatusCodes.Status409Conflict,               detail),
            NotFoundException           => (StatusCodes.Status404NotFound,               detail),
            ValidationException or InvalidOperationException => (StatusCodes.Status422UnprocessableEntity, detail),
            FormatException or JsonException or BadHttpRequestException => (StatusCodes.Status400BadRequest, detail),
            _                           => (StatusCodes.Status500InternalServerError,    string.IsNullOrWhiteSpace(detail) ? "Đã xảy ra lỗi hệ thống." : $"{ex.GetType().Name}: {detail}")
        };

        ctx.Response.StatusCode  = status;
        ctx.Response.ContentType = "application/json";

        // If ValidationException has field-level errors, include them in the response
        object body;
        if (ex is ValidationException validationEx && validationEx.Errors.Count > 0)
        {
            body = new { title, status, errors = validationEx.Errors, detail, exception = ex.GetType().Name };
        }
        else
        {
            body = new { title, status, detail, exception = ex.GetType().Name };
        }

        var json = JsonSerializer.Serialize(body);
        return ctx.Response.WriteAsync(json);
    }
}
