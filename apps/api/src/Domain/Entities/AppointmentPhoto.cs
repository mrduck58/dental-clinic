namespace DentalClinic.API.Domain.Entities;

/// <summary>
/// Ảnh chụp bằng tay (không có máy chụp X-quang/CT tích hợp) gắn với 1 buổi hẹn, kèm ghi chú tuỳ chọn.
/// Section phân biệt 2 khu vực hiển thị dùng chung 1 bảng:
///   "exam"             — ảnh chụp chiếu lúc khám (tab "Khám").
///   "material-request" — ảnh dấu răng/răng lợi... đính kèm khi gửi yêu cầu vật tư (tab "Vật tư").
/// </summary>
public class AppointmentPhoto
{
    public const string SectionExam = "exam";
    public const string SectionMaterialRequest = "material-request";

    public Guid Id { get; private set; }
    public Guid AppointmentId { get; private set; }
    public string Section { get; private set; } = string.Empty;
    public string Url { get; private set; } = string.Empty;
    public string? Note { get; private set; }
    public string UploadedBy { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }

    // Navigation
    public Appointment Appointment { get; private set; } = null!;

    private AppointmentPhoto() { }

    public static AppointmentPhoto Create(Guid appointmentId, string section, string url, string? note, string uploadedBy)
        => new()
        {
            Id = Guid.NewGuid(),
            AppointmentId = appointmentId,
            Section = section,
            Url = url,
            Note = note,
            UploadedBy = uploadedBy,
            CreatedAt = DateTimeOffset.UtcNow,
        };

    public void UpdateNote(string? note) => Note = note;
}
