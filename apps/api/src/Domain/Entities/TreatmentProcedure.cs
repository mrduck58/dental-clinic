namespace DentalClinic.API.Domain.Entities;

/// <summary>
/// Một bước trong quy trình điều trị chuẩn của một dịch vụ
/// (ví dụ niềng răng: 1. Gắn mắc cài → 2. Thay dây → 3. Tháo mắc cài).
/// </summary>
public class TreatmentProcedure
{
    public Guid Id { get; private set; }
    public Guid ServiceId { get; private set; }
    public int StepNumber { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int DurationMinutes { get; private set; } = 30;
    public bool IsRequired { get; private set; } = true;
    public string? Description { get; private set; }

    // Navigation property
    public Service Service { get; private set; } = null!;

    private TreatmentProcedure() { }

    public static TreatmentProcedure Create(
        Guid serviceId,
        int stepNumber,
        string name,
        int durationMinutes = 30,
        bool isRequired = true,
        string? description = null)
    {
        return new TreatmentProcedure
        {
            Id = Guid.NewGuid(),
            ServiceId = serviceId,
            StepNumber = stepNumber,
            Name = name,
            DurationMinutes = durationMinutes > 0 ? durationMinutes : 30,
            IsRequired = isRequired,
            Description = description
        };
    }

    public void Update(int stepNumber, string name, int durationMinutes = 30, bool isRequired = true, string? description = null)
    {
        StepNumber = stepNumber;
        Name = name;
        DurationMinutes = durationMinutes > 0 ? durationMinutes : 30;
        IsRequired = isRequired;
        Description = description;
    }
}
