namespace DentalClinic.API.Domain.Entities;

/// <summary>
/// Nhật ký ghi nhận lịch sử tiến độ từng lần của một bước điều trị thực tế (TreatmentSession).
/// </summary>
public class StepProgressEntry
{
    public Guid Id { get; private set; }
    public Guid TreatmentSessionId { get; private set; }
    public int CompletionPercentage { get; private set; }
    public string? Note { get; private set; }
    public DateTimeOffset RecordedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    // Navigation properties
    public TreatmentSession TreatmentSession { get; private set; } = null!;

    private StepProgressEntry() { }

    public static StepProgressEntry Create(
        Guid treatmentSessionId,
        int completionPercentage,
        string? note = null,
        DateTimeOffset? recordedAt = null)
    {
        return new StepProgressEntry
        {
            Id = Guid.NewGuid(),
            TreatmentSessionId = treatmentSessionId,
            CompletionPercentage = Math.Clamp(completionPercentage, 0, 100),
            Note = note,
            RecordedAt = recordedAt ?? DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}
