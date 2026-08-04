namespace DentalClinic.API.Domain.Entities;

/// <summary>Đánh giá của bệnh nhân dành cho nha sĩ — mỗi bệnh nhân chỉ có 1 đánh giá cho mỗi nha sĩ (gửi lại sẽ ghi đè).</summary>
public class DentistReview
{
    public Guid Id { get; private set; }
    public Guid DentistId { get; private set; }
    public Guid PatientId { get; private set; }
    public Guid? AppointmentId { get; private set; }
    public int Rating { get; private set; }
    public string Comment { get; private set; } = string.Empty;
    /// <summary>Nhãn nổi bật do bệnh nhân chọn (VD: "Không đau", "Chuyên nghiệp"), lưu dạng chuỗi phân tách bởi dấu phẩy.</summary>
    public string? TagsCsv { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public Dentist Dentist { get; private set; } = null!;
    public Patient Patient { get; private set; } = null!;
    public Appointment? Appointment { get; private set; }

    public IReadOnlyList<string> Tags =>
        string.IsNullOrWhiteSpace(TagsCsv) ? [] : TagsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private DentistReview() { }

    public static DentistReview Create(Guid dentistId, Guid patientId, int rating, string comment, IEnumerable<string>? tags = null, Guid? appointmentId = null)
    {
        var now = DateTimeOffset.UtcNow;
        return new DentistReview
        {
            Id = Guid.NewGuid(),
            DentistId = dentistId,
            PatientId = patientId,
            AppointmentId = appointmentId,
            Rating = rating,
            Comment = comment,
            TagsCsv = tags == null ? null : string.Join(',', tags),
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    public void Update(int rating, string comment, IEnumerable<string>? tags = null)
    {
        Rating = rating;
        Comment = comment;
        TagsCsv = tags == null ? null : string.Join(',', tags);
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
