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
            ForbiddenException          => (StatusCodes.Status403Forbidden,              ex.Message),
            ConflictException           => (StatusCodes.Status409Conflict,               ex.Message),
            NotFoundException           => (StatusCodes.Status404NotFound,               ex.Message),
            ValidationException or InvalidOperationException => (StatusCodes.Status422UnprocessableEntity, ex.Message),
            FormatException or JsonException or BadHttpRequestException => (StatusCodes.Status400BadRequest, ex.Message),
            _                           => (StatusCodes.Status500InternalServerError,    string.IsNullOrWhiteSpace(ex.Message) ? "Đã xảy ra lỗi hệ thống." : $"{ex.GetType().Name}: {ex.Message}")
        };

        ctx.Response.StatusCode  = status;
        ctx.Response.ContentType = "application/json";

        // If ValidationException has field-level errors, include them in the response
        object body;
        if (ex is ValidationException validationEx && validationEx.Errors.Count > 0)
        {
            body = new { title, status, errors = validationEx.Errors, detail = ex.Message, exception = ex.GetType().Name };
        }
        else
        {
            body = new { title, status, detail = ex.Message, exception = ex.GetType().Name };
        }

        var json = JsonSerializer.Serialize(body);
        return ctx.Response.WriteAsync(json);
    }
}
