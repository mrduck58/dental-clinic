namespace DentalClinic.API.Domain.Entities;

public enum MaterialRequestStatus
{
    Pending = 1,  // Bác sĩ đã yêu cầu, chờ kho xử lý
    Done = 2      // Kho đã nhập / xử lý xong
}

/// <summary>
/// Yêu cầu vật liệu/vật tư do bác sĩ ghi khi lập liệu trình dài hạn.
/// Được gửi sang trang nhập–xuất vật tư của staff để nhập kho.
/// </summary>
public class MaterialRequest
{
    public Guid Id { get; private set; }
    public Guid? CourseId { get; private set; }
    public string CourseName { get; private set; } = string.Empty;
    public string PatientName { get; private set; } = string.Empty;
    public string DentistName { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty; // danh sách vật liệu (text)
    public MaterialRequestStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? HandledAt { get; private set; }
    public string? HandledBy { get; private set; }

    private MaterialRequest() { }

    public static MaterialRequest Create(string courseName, string patientName, string dentistName, string content, Guid? courseId = null)
        => new()
        {
            Id = Guid.NewGuid(),
            CourseId = courseId,
            CourseName = courseName,
            PatientName = patientName,
            DentistName = dentistName,
            Content = content,
            Status = MaterialRequestStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };

    public void MarkDone(string handledBy)
    {
        Status = MaterialRequestStatus.Done;
        HandledAt = DateTimeOffset.UtcNow;
        HandledBy = handledBy;
    }
}
