namespace DentalClinic.API.Domain.Constants;

public static class NotificationType
{
    public const string System      = "system";
    public const string Account     = "account";
    public const string Schedule    = "schedule";
    public const string Service     = "service";
    public const string Security    = "security";
    public const string Reminder    = "reminder";
    public const string Appointment = "appointment";
    public const string Invoice     = "invoice";
}

public static class NotificationPriority
{
    public const string High   = "high";
    public const string Medium = "medium";
    public const string Low    = "low";
}
