using System.Security.Claims;
using System.Text.Json;
using DentalClinic.API.Domain.Interfaces.Repositories;

namespace DentalClinic.API.Presentation.Middlewares;

/// <summary>
/// Runs after UseAuthentication (so ctx.User is populated) and before UseAuthorization.
/// A valid, unexpired JWT only proves who the user WAS at login time — it says nothing about
/// whether the account has since been disabled or had its role changed. Without this check, an
/// Admin locking/disabling an account has no effect until the user's existing token naturally
/// expires. This re-validates account state on every authenticated request.
/// </summary>
public class AccountStatusMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext ctx, IUserRepository userRepository)
    {
        if (ctx.User.Identity?.IsAuthenticated == true)
        {
            var idClaim = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var roleClaim = ctx.User.FindFirstValue(ClaimTypes.Role);

            if (!Guid.TryParse(idClaim, out var userId))
            {
                await RejectAsync(ctx);
                return;
            }

            var status = await userRepository.GetAccountStatusAsync(userId, ctx.RequestAborted);
            if (status is null || !status.IsActive || status.Role != roleClaim)
            {
                await RejectAsync(ctx);
                return;
            }
        }

        await next(ctx);
    }

    private static Task RejectAsync(HttpContext ctx)
    {
        ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
        ctx.Response.ContentType = "application/json";
        var body = JsonSerializer.Serialize(new { title = "Tài khoản đã bị khóa hoặc không còn hợp lệ.", status = 401 });
        return ctx.Response.WriteAsync(body);
    }
}
