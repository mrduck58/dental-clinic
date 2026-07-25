namespace DentalClinic.API.Domain.Entities;

public class ActivityLog
{
    // int PK is intentional for audit logs: sequential, insert-ordered, avoids UUID fragmentation
    public int Id { get; private set; }
    public Guid? UserId { get; private set; }
    public string UserName { get; private set; } = string.Empty;
    public string UserRole { get; private set; } = string.Empty;
    public string Action { get; private set; } = string.Empty;
    public string Module { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string? IpAddress { get; private set; }
    public string Status { get; private set; } = string.Empty;
    public string? TargetId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private ActivityLog() { }

    public static ActivityLog Create(
        Guid? userId,
        string userName,
        string userRole,
        string action,
        string module,
        string description,
        string status,
        string? ipAddress = null,
        string? targetId = null)
    {
        return new ActivityLog
        {
            UserId = userId,
            UserName = userName,
            UserRole = userRole,
            Action = action,
            Module = module,
            Description = description,
            Status = status,
            IpAddress = ipAddress,
            TargetId = targetId,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}
