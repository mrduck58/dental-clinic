namespace DentalClinic.API.Domain.Entities;

/// <summary>
/// Một bước trong quy trình điều trị chuẩn của một dịch vụ
/// (ví dụ niềng răng: 1. Gắn mắc cài 30% → 2. Thay dây 40% → 3. Tháo mắc cài 30%).
/// </summary>
public class TreatmentProcedure
{
    public Guid Id { get; private set; }
    public Guid ServiceId { get; private set; }
    public int StepNumber { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int PercentOfTotal { get; private set; } // % tiến độ của bước so với toàn liệu trình

    // Navigation property
    public Service Service { get; private set; } = null!;

    private TreatmentProcedure() { }

    public static TreatmentProcedure Create(Guid serviceId, int stepNumber, string name, int percentOfTotal)
    {
        return new TreatmentProcedure
        {
            Id = Guid.NewGuid(),
            ServiceId = serviceId,
            StepNumber = stepNumber,
            Name = name,
            PercentOfTotal = Math.Clamp(percentOfTotal, 0, 100)
        };
    }

    public void Update(int stepNumber, string name, int percentOfTotal)
    {
        StepNumber = stepNumber;
        Name = name;
        PercentOfTotal = Math.Clamp(percentOfTotal, 0, 100);
    }
}
