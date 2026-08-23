namespace DentalClinic.API.Domain.Entities;

public enum MaterialRequestStatus
{
    Pending = 1,  // Vừa tạo yêu cầu, chưa xử lý gì
    Ordered = 2,  // Staff đã đặt hàng nhà cung cấp/lab, đang chờ hàng về — CHƯA nhập kho
    Done = 3      // Đã nhận hàng thật và nhập kho
}

/// <summary>
/// Yêu cầu vật liệu/vật tư do bác sĩ ghi khi lập liệu trình dài hạn.
/// Được gửi sang trang nhập–xuất vật tư của staff để nhập kho.
/// </summary>
public class MaterialRequest
{
    public Guid Id { get; private set; }
    public Guid? PatientId { get; private set; }
    public string CourseName { get; private set; } = string.Empty;
    public string PatientName { get; private set; } = string.Empty;
    public string DentistName { get; private set; } = string.Empty;
    public MaterialRequestStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? OrderedAt { get; private set; }
    public string? OrderedBy { get; private set; }
    public string? SupplierNote { get; private set; }
    public DateTimeOffset? HandledAt { get; private set; }
    public string? HandledBy { get; private set; }

    public Guid? DentistId { get; private set; }
    public DentistProfile? Dentist { get; private set; }

    // Dịch vụ cụ thể trong liệu trình mà yêu cầu này phục vụ (vd đặt mão sứ cho đúng lần "Bọc Răng Sứ"
    // này) — null nếu tạo tự do, không gắn dịch vụ nào cụ thể (vd staff tự đặt trước cho ca hẹn sắp tới).
    // Dùng để: xóa dịch vụ khỏi liệu trình thì dọn theo yêu cầu Pending liên quan (xem DeleteTreatmentPlanHandler).
    public Guid? TreatmentPlanId { get; private set; }

    // Buổi hẹn đã sinh ra yêu cầu này — null nếu staff tự khởi tạo (không qua buổi khám). Dùng để lấy
    // đúng ảnh đính kèm (dấu răng, răng lợi...) bác sĩ đã chụp cho buổi đó, xem AppointmentPhoto.Section.
    public Guid? AppointmentId { get; private set; }

    private readonly List<MaterialRequestItem> _items = [];
    public IReadOnlyCollection<MaterialRequestItem> Items => _items;

    private MaterialRequest() { }

    public static MaterialRequest Create(
        string courseName, string patientName, string dentistName,
        IEnumerable<(string ItemName, string? Detail, int Quantity, string Unit)> items,
        Guid? patientId = null, Guid? treatmentPlanId = null, Guid? appointmentId = null)
    {
        var request = new MaterialRequest
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            CourseName = courseName,
            PatientName = patientName,
            DentistName = dentistName,
            TreatmentPlanId = treatmentPlanId,
            AppointmentId = appointmentId,
            Status = MaterialRequestStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };
        request._items.AddRange(items.Select(i => MaterialRequestItem.Create(request.Id, i.ItemName, i.Detail, i.Quantity, i.Unit)));
        return request;
    }

    public void MarkOrdered(string orderedBy, string? supplierNote)
    {
        Status = MaterialRequestStatus.Ordered;
        OrderedAt = DateTimeOffset.UtcNow;
        OrderedBy = orderedBy;
        SupplierNote = supplierNote;
    }

    public void MarkDone(string handledBy)
    {
        Status = MaterialRequestStatus.Done;
        HandledAt = DateTimeOffset.UtcNow;
        HandledBy = handledBy;
    }
}
