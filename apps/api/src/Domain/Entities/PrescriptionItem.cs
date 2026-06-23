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
    public DateTimeOffset CreatedAt { get; private set; }

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
        string? notes = null)
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
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void Update(string medicineName, string dosage, int quantity, string unit, string usage, string? notes)
    {
        MedicineName = medicineName;
        Dosage = dosage;
        Quantity = quantity;
        Unit = unit;
        Usage = usage;
        Notes = notes;
    }
}
