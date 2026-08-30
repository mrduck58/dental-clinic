namespace DentalClinic.API.Domain.Entities;

using DentalClinic.API.Domain.Enums;

/// <summary>
/// Kế hoạch điều trị tổng thể của một bệnh nhân, chứa một hoặc nhiều mục dịch vụ chỉ định (TreatmentPlanItem).
/// </summary>
public class TreatmentPlan
{
    public Guid Id { get; private set; }
    public Guid PatientId { get; private set; }
    public Guid DentistId { get; private set; }
    public Guid? AppointmentId { get; private set; } // Buổi hẹn lập kế hoạch
    public string Title { get; private set; } = string.Empty;
    public TreatmentPlanStatus Status { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    public decimal TotalCost => Items.Where(i => i.Status != TreatmentPlanItemStatus.Cancelled).Sum(i => i.TotalCost);

    // Thuộc tính tương thích ngược cho item chính
    public Guid ServiceId => Items.FirstOrDefault()?.ServiceId ?? Guid.Empty;
    public Service Service => Items.FirstOrDefault()?.Service!;
    public string? ServiceOptionName => Items.FirstOrDefault()?.ServiceOptionName;
    public decimal UnitPrice => Items.FirstOrDefault()?.UnitPrice ?? 0m;
    public int Quantity => Items.FirstOrDefault()?.Quantity ?? 1;
    public string? Teeth => Items.FirstOrDefault()?.Teeth;
    public DateOnly? WarrantyUntil => Items.FirstOrDefault()?.WarrantyUntil;
    public string? StepProgressJson => null;

    // Navigation properties
    public Patient Patient { get; private set; } = null!;
    public DentistProfile Dentist { get; private set; } = null!;
    public Appointment? Appointment { get; private set; }
    public ICollection<TreatmentPlanItem> Items { get; private set; } = new List<TreatmentPlanItem>();

    private TreatmentPlan() { }

    public static TreatmentPlan Create(
        Guid patientId,
        Guid dentistId,
        Guid? appointmentId,
        string? title = null,
        string? notes = null)
    {
        return new TreatmentPlan
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            DentistId = dentistId,
            AppointmentId = appointmentId,
            Title = string.IsNullOrWhiteSpace(title) ? "Kế hoạch điều trị" : title.Trim(),
            Notes = notes,
            Status = TreatmentPlanStatus.Planned,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public static TreatmentPlan Create(
        Guid patientId,
        Guid dentistId,
        Guid? appointmentId,
        Guid serviceId,
        decimal unitPrice,
        int quantity = 1,
        string? teeth = null,
        string? notes = null,
        DateOnly? warrantyUntil = null,
        string? serviceOptionName = null,
        Guid? serviceOptionId = null)
    {
        var plan = new TreatmentPlan
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            DentistId = dentistId,
            AppointmentId = appointmentId,
            Title = "Kế hoạch điều trị",
            Notes = notes,
            Status = TreatmentPlanStatus.Planned,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var item = TreatmentPlanItem.Create(
            plan.Id,
            serviceId,
            unitPrice,
            quantity,
            teeth,
            notes,
            warrantyUntil,
            serviceOptionId,
            serviceOptionName);

        plan.Items.Add(item);
        return plan;
    }

    public void Update(string title, string? notes)
    {
        Title = string.IsNullOrWhiteSpace(title) ? "Kế hoạch điều trị" : title.Trim();
        Notes = notes;
    }

    public void Update(
        decimal unitPrice,
        int quantity,
        string? teeth,
        string? notes,
        DateOnly? warrantyUntil)
    {
        Notes = notes;
        var item = Items.FirstOrDefault();
        if (item != null)
        {
            item.Update(unitPrice, quantity, teeth, notes, warrantyUntil);
        }
    }

    public void SetStatus(TreatmentPlanStatus status)
    {
        Status = status;
        CompletedAt = status == TreatmentPlanStatus.Completed ? DateTimeOffset.UtcNow : null;
    }

    public void UpdateStepProgress(string? json) { }
}
