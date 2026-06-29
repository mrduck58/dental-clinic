using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DentalClinic.API.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Http;

namespace DentalClinic.API.Infrastructure.Services;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public Guid? UserId
    {
        get
        {
            var value = User?.FindFirstValue(JwtRegisteredClaimNames.Sub)
                     ?? User?.FindFirstValue(ClaimTypes.NameIdentifier);
            return value is not null && Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public string UserName =>
        User?.FindFirstValue(JwtRegisteredClaimNames.Name)
        ?? User?.FindFirstValue(ClaimTypes.Name)
        ?? User?.FindFirstValue(JwtRegisteredClaimNames.Email)
        ?? "system";

    public string UserRole =>
        User?.FindFirstValue(ClaimTypes.Role)
        ?? "Unknown";

    public string? IpAddress
    {
        get
        {
            var ctx = httpContextAccessor.HttpContext;
            if (ctx is null) return null;

            var forwarded = ctx.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(forwarded))
                return forwarded.Split(',')[0].Trim();

            return ctx.Connection.RemoteIpAddress?.ToString();
        }
    }
}
