using DentalClinic.API.Domain.Exceptions;

namespace DentalClinic.API.Domain.Entities;

/// <summary>
/// Quy tắc hoa hồng theo % trên doanh thu đã thu. Chỉ áp dụng chính xác cho nha sĩ (DentistId trỏ tới
/// DentistProfile.Id, giống Appointment.DentistId) vì đó là chiều duy nhất doanh thu gắn trực tiếp được
/// vào một người trong dữ liệu hiện có (qua Appointment → Invoice). Doanh thu chưa gắn trực tiếp theo
/// từng nhân viên (Staff) nên quy tắc áp dụng cho Staff chỉ mang tính ghi nhận, không tự tính được số tiền.
/// </summary>
public class CommissionRule
{
    public Guid Id { get; private set; }

    // null = áp dụng cho mọi nha sĩ.
    public Guid? DentistId { get; private set; }

    // null = áp dụng cho mọi dịch vụ. So khớp theo tên snapshot trên InvoiceItem (không có ServiceId chuẩn).
    public string? ServiceName { get; private set; }

    public decimal RatePercent { get; private set; }
    public DateOnly EffectiveFrom { get; private set; }
    public DateOnly? EffectiveTo { get; private set; }
    public bool IsActive { get; private set; }
    public string? Note { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private CommissionRule() { }

    public static CommissionRule Create(
        Guid? dentistId,
        string? serviceName,
        decimal ratePercent,
        DateOnly effectiveFrom,
        DateOnly? effectiveTo,
        string? note)
    {
        Validate(ratePercent, effectiveFrom, effectiveTo);

        var now = DateTimeOffset.UtcNow;
        return new CommissionRule
        {
            Id = Guid.NewGuid(),
            DentistId = dentistId,
            ServiceName = string.IsNullOrWhiteSpace(serviceName) ? null : serviceName.Trim(),
            RatePercent = ratePercent,
            EffectiveFrom = effectiveFrom,
            EffectiveTo = effectiveTo,
            IsActive = true,
            Note = note,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    public void Update(
        Guid? dentistId,
        string? serviceName,
        decimal ratePercent,
        DateOnly effectiveFrom,
        DateOnly? effectiveTo,
        string? note)
    {
        Validate(ratePercent, effectiveFrom, effectiveTo);

        DentistId = dentistId;
        ServiceName = string.IsNullOrWhiteSpace(serviceName) ? null : serviceName.Trim();
        RatePercent = ratePercent;
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        Note = note;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Activate() { IsActive = true; UpdatedAt = DateTimeOffset.UtcNow; }
    public void Deactivate() { IsActive = false; UpdatedAt = DateTimeOffset.UtcNow; }

    /// <summary>Quy tắc có áp dụng cho một ngày cụ thể hay không (nằm trong [EffectiveFrom, EffectiveTo]).</summary>
    public bool IsEffectiveOn(DateOnly date) => date >= EffectiveFrom && (EffectiveTo is null || date <= EffectiveTo);

    private static void Validate(decimal ratePercent, DateOnly effectiveFrom, DateOnly? effectiveTo)
    {
        if (ratePercent <= 0 || ratePercent > 100)
            throw new ValidationException("Tỷ lệ hoa hồng phải trong khoảng lớn hơn 0 và nhỏ hơn hoặc bằng 100.");
        if (effectiveTo is DateOnly to && to < effectiveFrom)
            throw new ValidationException("Ngày kết thúc áp dụng không được trước ngày bắt đầu.");
    }
}
