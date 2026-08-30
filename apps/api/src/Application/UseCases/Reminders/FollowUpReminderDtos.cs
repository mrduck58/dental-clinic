namespace DentalClinic.API.Application.UseCases.Reminders;

public record SetFollowUpReminderRequest(
    DateOnly FollowUpDate,
    string? Note,
    Guid? TreatmentPlanItemId = null,
    Guid? TreatmentSessionId = null);

public class FollowUpReminderDto
{
    public Guid AppointmentId { get; set; }
    public DateOnly? FollowUpDate { get; set; }
    public string? FollowUpNote { get; set; }
    public Guid? FollowUpId { get; set; }
}

public class FollowUpDueDto
{
    public Guid OriginalAppointmentId { get; set; }
    public Guid? FollowUpId { get; set; }
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string? PatientPhone { get; set; }
    public DateOnly? PatientDateOfBirth { get; set; }
    public string? Gender { get; set; }
    public Guid DentistId { get; set; }
    public string DentistName { get; set; } = string.Empty;
    public Guid? ServiceId { get; set; }
    public string? ServiceName { get; set; }
    public Guid? PrefillServiceId { get; set; }
    public DateTimeOffset OriginalAppointmentDate { get; set; }
    public DateOnly? FollowUpDate { get; set; }
    public string? FollowUpNote { get; set; }
    public List<string> ActivePlans { get; set; } = new();
}

internal static class FollowUpReminderMapper
{
    public static FollowUpReminderDto ToDto(Guid appointmentId, DateOnly? date, string? note, Guid? followUpId = null) => new()
    {
        AppointmentId = appointmentId,
        FollowUpDate = date,
        FollowUpNote = note,
        FollowUpId = followUpId
    };
}
