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
    public DateOnly? PatientDateOfBirth { get; set; }
    public string? Gender { get; set; }
    public Guid DentistId { get; set; }
    public string DentistName { get; set; } = string.Empty;
    /// <summary>Dịch vụ đã đặt ở buổi hẹn gốc — hiển thị ở "Buổi gần nhất", KHÔNG dùng để điền form.</summary>
    public Guid? ServiceId { get; set; }
    public string? ServiceName { get; set; }
    /// <summary>Dịch vụ đang điều trị (liệu trình InProgress) — dùng để điền sẵn form đặt lịch khi
    /// staff check-in tái khám; rơi về ServiceId nếu không còn liệu trình nào đang thực hiện.</summary>
    public Guid? PrefillServiceId { get; set; }
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
