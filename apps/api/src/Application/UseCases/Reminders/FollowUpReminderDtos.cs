namespace DentalClinic.API.Application.UseCases.Reminders;

/// <summary>Body của PUT api/appointments/{id}/follow-up-reminder — giữ nguyên hình dạng JSON.</summary>
public record SetFollowUpReminderRequest(DateOnly FollowUpDate, string? Note);

public class FollowUpReminderDto
{
    public Guid AppointmentId { get; set; }
    public DateOnly? FollowUpDate { get; set; }
    public string? FollowUpNote { get; set; }
}

/// <summary>Một bệnh nhân đang trong diện chờ tái khám (bác sĩ đã hẹn ngày tái khám sau khi kết thúc điều trị).</summary>
public class FollowUpDueDto
{
    public Guid OriginalAppointmentId { get; set; }
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string? PatientPhone { get; set; }
    public string? Gender { get; set; }
    public string DentistName { get; set; } = string.Empty;
    public string? ServiceName { get; set; }
    public DateTimeOffset OriginalAppointmentDate { get; set; }
    public DateOnly? FollowUpDate { get; set; }
    public string? FollowUpNote { get; set; }
    public List<string> ActivePlans { get; set; } = new(); // Các liệu trình đang thực hiện
}

internal static class FollowUpReminderMapper
{
    public static FollowUpReminderDto ToDto(Guid appointmentId, DateOnly? date, string? note) => new()
    {
        AppointmentId = appointmentId,
        FollowUpDate = date,
        FollowUpNote = note
    };
}
