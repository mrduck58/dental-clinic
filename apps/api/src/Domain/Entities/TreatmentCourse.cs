using DentalClinic.API.Domain.Enums;

namespace DentalClinic.API.Domain.Entities;

/// <summary>
/// Liệu trình điều trị dài hạn (niềng răng, implant…) trải qua nhiều buổi hẹn.
/// Tổng chi phí được chốt ngay từ buổi đầu; bệnh nhân trả thành nhiều đợt
/// (cọc + các đợt sau mỗi lần tái khám + tất toán cuối) — mỗi đợt là một Invoice
/// gắn với CourseId. Công nợ = TotalCost − tổng các đợt đã thu.
/// </summary>
public class TreatmentCourse
{
    public Guid Id { get; private set; }
    public Guid PatientId { get; private set; }
    public Guid DentistId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public decimal TotalCost { get; private set; }
    public TreatmentCourseStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    // Navigation properties
    public Patient Patient { get; private set; } = null!;
    public Dentist Dentist { get; private set; } = null!;
    public ICollection<Appointment> Appointments { get; private set; } = new List<Appointment>();
    public ICollection<Invoice> Invoices { get; private set; } = new List<Invoice>();

    private TreatmentCourse() { }

    public static TreatmentCourse Create(Guid patientId, Guid dentistId, string name, decimal totalCost)
    {
        return new TreatmentCourse
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            DentistId = dentistId,
            Name = name,
            TotalCost = totalCost < 0 ? 0 : totalCost,
            Status = TreatmentCourseStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void Update(string name, decimal totalCost)
    {
        Name = name;
        TotalCost = totalCost < 0 ? 0 : totalCost;
    }

    public void Complete()
    {
        Status = TreatmentCourseStatus.Completed;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    public void Cancel() => Status = TreatmentCourseStatus.Cancelled;
}
