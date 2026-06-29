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

public static class ActivityAction
{
    public const string Login    = "login";
    public const string Create   = "create";
    public const string Edit     = "edit";
    public const string Delete   = "delete";
    public const string Export   = "export";
    public const string View     = "view";
    public const string Approve  = "approve";
    public const string Reject   = "reject";
    public const string Cancel   = "cancel";
    public const string Payment  = "payment";
}

public static class ActivityModule
{
    public const string Account     = "account";
    public const string Appointment = "appointment";
    public const string Service     = "service";
    public const string Post        = "post";
    public const string Schedule    = "schedule";
    public const string Room        = "room";
    public const string Medicine    = "medicine";
    public const string Inventory   = "inventory";
    public const string Leave       = "leave";
    public const string Invoice     = "invoice";
    public const string Feedback    = "feedback";
    public const string Promotion   = "promotion";
    public const string System      = "system";
}

public static class ActivityStatus
{
    public const string Success = "success";
    public const string Failed  = "failed";
    public const string Warning = "warning";
}
