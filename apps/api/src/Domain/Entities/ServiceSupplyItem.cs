namespace DentalClinic.API.Domain.Entities;

/// <summary>
/// Định mức vật tư chuẩn của một dịch vụ (BOM) — dịch vụ X thường dùng bao nhiêu đơn vị vật tư Y.
/// Chỉ là gợi ý mặc định để bác sĩ điền nhanh khi ghi nhận tiêu hao thực tế (xem TreatmentSupplyUsage),
/// không tự trừ kho — trừ kho chỉ xảy ra khi tiêu hao thực tế được ghi nhận.
/// </summary>
public class ServiceSupplyItem
{
    public Guid Id { get; private set; }
    public Guid ServiceId { get; private set; }
    // Tên option (ServiceOption.Name) mà dòng định mức này áp dụng riêng — null = áp dụng CHUNG cho mọi
    // option của dịch vụ (vd: găng tay, khẩu trang). SNAPSHOT theo tên như TreatmentPlan.ServiceOptionName,
    // không phải FK tới ServiceOption.Id, vì lý do tương tự (ServiceOption bị xoá/tạo lại toàn bộ mỗi khi
    // sửa dịch vụ). Khi bác sĩ chọn option cụ thể, danh sách hiệu lực = các dòng chung + các dòng khớp tên option đó.
    public string? ServiceOptionName { get; private set; }
    public Guid SupplyItemId { get; private set; }
    public int DefaultQuantity { get; private set; }

    // Navigation
    public Service Service { get; private set; } = null!;
    public SupplyItem SupplyItem { get; private set; } = null!;

    private ServiceSupplyItem() { }

    public static ServiceSupplyItem Create(Guid serviceId, Guid supplyItemId, int defaultQuantity, string? serviceOptionName = null)
        => new()
        {
            Id = Guid.NewGuid(),
            ServiceId = serviceId,
            ServiceOptionName = serviceOptionName,
            SupplyItemId = supplyItemId,
            DefaultQuantity = defaultQuantity,
        };
}
