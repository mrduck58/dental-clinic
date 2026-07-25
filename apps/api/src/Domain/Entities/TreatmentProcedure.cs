namespace DentalClinic.API.Domain.Entities;

/// <summary>
/// Một bước trong quy trình điều trị chuẩn của một dịch vụ
/// (ví dụ niềng răng: 1. Gắn mắc cài → 2. Thay dây → 3. Tháo mắc cài).
/// Chỉ là danh sách tên các bước; % tiến độ do bác sĩ nhập khi ghi nhận quá trình điều trị.
/// </summary>
public class TreatmentProcedure
{
    public Guid Id { get; private set; }
    public Guid ServiceId { get; private set; }
    public int StepNumber { get; private set; }
    public string Name { get; private set; } = string.Empty;

    // Navigation property
    public Service Service { get; private set; } = null!;

    private TreatmentProcedure() { }

    public static TreatmentProcedure Create(Guid serviceId, int stepNumber, string name)
    {
        return new TreatmentProcedure
        {
            Id = Guid.NewGuid(),
            ServiceId = serviceId,
            StepNumber = stepNumber,
            Name = name
        };
    }

    public void Update(int stepNumber, string name)
    {
        StepNumber = stepNumber;
        Name = name;
    }
}
