namespace DentalClinic.API.Domain.Entities;

public class PrescriptionItem
{
    public Guid Id { get; private set; }
    public Guid PrescriptionId { get; private set; }
    public string MedicineName { get; private set; } = string.Empty;
    public string Dosage { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public string Unit { get; private set; } = string.Empty;
    public string Usage { get; private set; } = string.Empty;
    public string? Notes { get; private set; }
    /// <summary>Số lần uống mỗi ngày — dùng để sinh lịch nhắc uống thuốc thật trên mobile. Null nếu bác sĩ không nhập.</summary>
    public int? TimesPerDay { get; private set; }
    /// <summary>Số ngày uống thuốc, tính từ <see cref="StartDate"/>.</summary>
    public int? DurationDays { get; private set; }
    /// <summary>Ngày bắt đầu uống — mặc định là ngày kê đơn nếu bác sĩ không chỉnh.</summary>
    public DateOnly? StartDate { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public Guid? MedicineId { get; private set; }
    public Medicine? Medicine { get; private set; }

    // Navigation property
    public Prescription Prescription { get; private set; } = null!;

    private PrescriptionItem() { }

    public static PrescriptionItem Create(
        Guid prescriptionId,
        string medicineName,
        string dosage,
        int quantity,
        string unit,
        string usage,
        string? notes = null,
        int? timesPerDay = null,
        int? durationDays = null,
        DateOnly? startDate = null)
    {
        return new PrescriptionItem
        {
            Id = Guid.NewGuid(),
            PrescriptionId = prescriptionId,
            MedicineName = medicineName,
            Dosage = dosage,
            Quantity = quantity,
            Unit = unit,
            Usage = usage,
            Notes = notes,
            TimesPerDay = timesPerDay,
            DurationDays = durationDays,
            StartDate = startDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void Update(
        string medicineName, string dosage, int quantity, string unit, string usage, string? notes,
        int? timesPerDay = null, int? durationDays = null, DateOnly? startDate = null)
    {
        MedicineName = medicineName;
        Dosage = dosage;
        Quantity = quantity;
        Unit = unit;
        Usage = usage;
        Notes = notes;
        TimesPerDay = timesPerDay;
        DurationDays = durationDays;
        StartDate = startDate ?? StartDate;
    }
}
