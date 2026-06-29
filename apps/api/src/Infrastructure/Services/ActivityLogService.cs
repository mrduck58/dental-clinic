using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace DentalClinic.API.Infrastructure.Services;

public class ActivityLogService(
    IActivityLogRepository repository,
    ILogger<ActivityLogService> logger) : IActivityLogService
{
    public async Task LogAsync(
        Guid? userId,
        string userName,
        string userRole,
        string action,
        string module,
        string description,
        string status,
        string? ipAddress = null,
        string? targetId = null,
        CancellationToken ct = default)
    {
        try
        {
            var log = ActivityLog.Create(userId, userName, userRole, action, module, description, status, ipAddress, targetId);
            await repository.AddAsync(log, ct);
        }
        catch (Exception ex)
        {
            // Không để lỗi ghi log làm hỏng luồng chính
            logger.LogWarning(ex, "Failed to write activity log: {Action} {Module}", action, module);
        }
    }
}
