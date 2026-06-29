namespace DentalClinic.API.Domain.Interfaces.Services;

public interface IActivityLogService
{
    Task LogAsync(
        Guid? userId,
        string userName,
        string userRole,
        string action,
        string module,
        string description,
        string status,
        string? ipAddress = null,
        string? targetId = null,
        CancellationToken ct = default);
}
