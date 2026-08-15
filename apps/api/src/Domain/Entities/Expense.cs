using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;

namespace DentalClinic.API.Domain.Entities;

/// <summary>
/// Một khoản chi phí tự nhập tay (không phải vật tư/lương — hai khoản đó có nguồn dữ liệu riêng).
/// Có thể đánh dấu định kỳ (IsRecurring) để dùng làm mẫu sinh bản ghi cho các kỳ sau
/// (<see cref="RecurringSourceId"/> trên bản ghi con trỏ về bản ghi mẫu, giống cơ chế Invoice.ParentInvoiceId).
/// </summary>
public class Expense
{
    public Guid Id { get; private set; }
    public ExpenseCategory Category { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public DateOnly Date { get; private set; }
    public string? Note { get; private set; }

    public bool IsRecurring { get; private set; }
    public RecurrenceFrequency? Frequency { get; private set; }

    // Trỏ về bản ghi mẫu định kỳ nếu đây là bản ghi được sinh ra cho một kỳ cụ thể.
    public Guid? RecurringSourceId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private Expense() { }

    public static Expense Create(
        ExpenseCategory category,
        string description,
        decimal amount,
        DateOnly date,
        string? note,
        bool isRecurring,
        RecurrenceFrequency? frequency)
    {
        Validate(description, amount, isRecurring, frequency);

        var now = DateTimeOffset.UtcNow;
        return new Expense
        {
            Id = Guid.NewGuid(),
            Category = category,
            Description = description.Trim(),
            Amount = amount,
            Date = date,
            Note = note,
            IsRecurring = isRecurring,
            Frequency = isRecurring ? frequency : null,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    /// <summary>Sinh bản ghi chi phí của một kỳ cụ thể từ bản ghi mẫu định kỳ.</summary>
    public static Expense CreateRecurrenceInstance(Expense source, DateOnly date)
    {
        var now = DateTimeOffset.UtcNow;
        return new Expense
        {
            Id = Guid.NewGuid(),
            Category = source.Category,
            Description = source.Description,
            Amount = source.Amount,
            Date = date,
            Note = source.Note,
            IsRecurring = false,
            Frequency = null,
            RecurringSourceId = source.Id,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    public void Update(
        ExpenseCategory category,
        string description,
        decimal amount,
        DateOnly date,
        string? note,
        bool isRecurring,
        RecurrenceFrequency? frequency)
    {
        Validate(description, amount, isRecurring, frequency);

        Category = category;
        Description = description.Trim();
        Amount = amount;
        Date = date;
        Note = note;
        IsRecurring = isRecurring;
        Frequency = isRecurring ? frequency : null;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static void Validate(string description, decimal amount, bool isRecurring, RecurrenceFrequency? frequency)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ValidationException("Nội dung chi phí không được để trống.");
        if (amount <= 0)
            throw new ValidationException("Số tiền chi phí phải lớn hơn 0.");
        if (isRecurring && frequency is null)
            throw new ValidationException("Chi phí định kỳ phải chọn chu kỳ lặp lại.");
    }
}
