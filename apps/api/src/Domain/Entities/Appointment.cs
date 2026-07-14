using DentalClinic.API.Domain.Enums;

namespace DentalClinic.API.Domain.Entities;

public class Appointment
{
    public Guid Id { get; private set; }
    public Guid PatientId { get; private set; }
    public Guid DentistId { get; private set; }
    public Guid? ServiceId { get; private set; }
    public DateTimeOffset AppointmentDate { get; private set; }
    public AppointmentStatus Status { get; private set; }
    public string? Notes { get; private set; }
    public string? Symptoms { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Thời điểm bệnh nhân check-in — quyết định thứ tự trong hàng đợi (null nếu chưa check-in).</summary>
    public DateTimeOffset? CheckedInAt { get; private set; }

    /// <summary>Tóm tắt lịch sử khám do AI tạo, gắn với LẦN KHÁM NÀY (dựa trên các lịch hẹn trước đó
    /// của cùng bệnh nhân). Cache lại để không gọi Gemini lại mỗi lần bác sĩ mở trang nếu dữ liệu
    /// chưa đổi — xem <see cref="AiSummaryBasedOnCount"/> để biết cache còn hợp lệ hay không.</summary>
    public string? AiSummary { get; private set; }
    public DateTimeOffset? AiSummaryGeneratedAt { get; private set; }

    /// <summary>Số lịch hẹn trước đó đã dùng để tạo <see cref="AiSummary"/> — nếu số này khác với số
    /// lịch hẹn trước đó hiện tại (có thêm lịch mới), cache coi như cũ và phải tạo lại.</summary>
    public int? AiSummaryBasedOnCount { get; private set; }

    // Navigation properties
    public Patient Patient { get; private set; } = null!;
    public Dentist Dentist { get; private set; } = null!;
    public Service? Service { get; private set; }
    public ICollection<Invoice> Invoices { get; private set; } = new List<Invoice>();
    public MedicalRecord? MedicalRecord { get; private set; }

    // Examination related
    public ICollection<Diagnosis> Diagnoses { get; private set; } = new List<Diagnosis>();
    public ICollection<TreatmentPlan> TreatmentPlans { get; private set; } = new List<TreatmentPlan>();
    public ICollection<Prescription> Prescriptions { get; private set; } = new List<Prescription>();

    // Follow-up appointments (dữ liệu lịch sử — luồng tạo lịch tái khám cũ đã bỏ)
    public Guid? FollowUpFromAppointmentId { get; private set; }
    public Appointment? FollowUpFromAppointment { get; private set; }
    public ICollection<Appointment> FollowUpAppointments { get; private set; } = new List<Appointment>();

    // Nhắc tái khám: chỉ hẹn ngày khám lại, không đặt lịch mới.
    // Khi bác sĩ kết thúc điều trị, hệ thống gửi thông báo cho bệnh nhân.
    public DateOnly? FollowUpDate { get; private set; }
    public string? FollowUpNote { get; private set; }

    private Appointment() { }

    public static Appointment Create(
        Guid patientId,
        Guid dentistId,
        DateTimeOffset appointmentDate,
        string? symptoms = null,
        Guid? serviceId = null,
        string? notes = null)
    {
        return new Appointment
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            DentistId = dentistId,
            ServiceId = serviceId,
            AppointmentDate = appointmentDate,
            Status = AppointmentStatus.Pending,
            Symptoms = symptoms,
            Notes = notes,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void Confirm() => Status = AppointmentStatus.Confirmed;
    public void CheckIn()
    {
        Status = AppointmentStatus.CheckedIn;
        CheckedInAt = DateTimeOffset.UtcNow;
    }
    /// <summary>Ghi nhận bệnh nhân vắng mặt — đã xác nhận nhưng không đến khám.</summary>
    public void MarkNoShow() => Status = AppointmentStatus.NoShow;
    public void StartTreatment() => Status = AppointmentStatus.InProgress;
    public void EndTreatment() => Status = AppointmentStatus.PendingPayment;
    public void Complete() => Status = AppointmentStatus.Completed;
    public void Cancel(string? reason = null)
    {
        Status = AppointmentStatus.Cancelled;
        if (!string.IsNullOrEmpty(reason))
        {
            Notes = string.IsNullOrEmpty(Notes) ? $"Lý do hủy: {reason}" : $"{Notes} | Lý do hủy: {reason}";
        }
    }

    /// <summary>
    /// Chuyển buổi hẹn sang bác sĩ khác. Dùng khi lễ tân kéo bệnh nhân đang chờ
    /// sang hàng đợi của phòng khác — phòng được quyết định bởi bác sĩ phụ trách.
    /// Giữ nguyên <see cref="CheckedInAt"/> để bệnh nhân không mất lượt ở hàng đợi mới.
    /// </summary>
    public void ReassignDentist(Guid dentistId) => DentistId = dentistId;

    /// <summary>Đặt hoặc xóa lịch hẹn tái khám (chỉ nhắc ngày, không tạo lịch hẹn mới).</summary>
    public void SetFollowUpReminder(DateOnly? date, string? note)
    {
        FollowUpDate = date;
        FollowUpNote = date == null ? null : note;
    }

    /// <summary>Lưu kết quả tóm tắt AI mới tạo cùng thời điểm và số lịch hẹn đã dùng làm căn cứ,
    /// để lần sau xác định được cache còn hợp lệ hay không (xem <see cref="AiSummaryBasedOnCount"/>).</summary>
    public void SetAiSummary(string summary, int basedOnPastAppointmentCount)
    {
        AiSummary = summary;
        AiSummaryGeneratedAt = DateTimeOffset.UtcNow;
        AiSummaryBasedOnCount = basedOnPastAppointmentCount;
    }

    /// <summary>
    /// Staff check-in bệnh nhân đến tái khám (từ tab Tái khám ở quầy): tạo buổi hẹn mới
    /// đã check-in ngay, gắn về buổi gốc qua <see cref="FollowUpFromAppointmentId"/> —
    /// đây là căn cứ để phía bác sĩ đánh dấu buổi này là tái khám.
    /// </summary>
    public static Appointment CheckInFollowUp(
        Guid originalAppointmentId,
        Guid patientId,
        Guid dentistId,
        Guid? serviceId = null,
        string? symptoms = null)
    {
        var now = DateTimeOffset.UtcNow;
        return new Appointment
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            DentistId = dentistId,
            ServiceId = serviceId,
            AppointmentDate = now,
            Status = AppointmentStatus.CheckedIn,
            CheckedInAt = now,
            Symptoms = symptoms,
            FollowUpFromAppointmentId = originalAppointmentId,
            CreatedAt = now
        };
    }
}
