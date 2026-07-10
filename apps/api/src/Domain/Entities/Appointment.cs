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

    // Liệu trình dài hạn (nếu buổi hẹn này thuộc một liệu trình nhiều buổi)
    public Guid? CourseId { get; private set; }
    public TreatmentCourse? Course { get; private set; }

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

    // Follow-up appointments
    public Guid? FollowUpFromAppointmentId { get; private set; }
    public Appointment? FollowUpFromAppointment { get; private set; }
    public ICollection<Appointment> FollowUpAppointments { get; private set; } = new List<Appointment>();

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

    /// <summary>Gắn buổi hẹn vào một liệu trình dài hạn.</summary>
    public void AssignToCourse(Guid courseId) => CourseId = courseId;

    /// <summary>
    /// Chuyển buổi hẹn sang bác sĩ khác. Dùng khi lễ tân kéo bệnh nhân đang chờ
    /// sang hàng đợi của phòng khác — phòng được quyết định bởi bác sĩ phụ trách.
    /// Giữ nguyên <see cref="CheckedInAt"/> để bệnh nhân không mất lượt ở hàng đợi mới.
    /// </summary>
    public void ReassignDentist(Guid dentistId) => DentistId = dentistId;

    public static Appointment CreateFollowUp(
        Guid originalAppointmentId,
        Guid patientId,
        Guid dentistId,
        DateTimeOffset appointmentDate,
        string? symptoms = null,
        Guid? serviceId = null,
        string? notes = null,
        Guid? courseId = null)
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
            FollowUpFromAppointmentId = originalAppointmentId,
            CourseId = courseId,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}
