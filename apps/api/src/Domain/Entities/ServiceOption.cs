namespace DentalClinic.API.Domain.Entities;

public class ServiceOption
{
    public Guid Id { get; private set; }
    public Guid ServiceId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public string Unit { get; private set; } = "Răng";
    public int SortOrder { get; private set; }

    // Navigation
    public Service Service { get; private set; } = null!;

    private ServiceOption() { }

    /// <summary>
    /// CỐ Ý không gán Id ở đây — để EF sinh khi thêm mới.
    ///
    /// ServiceOption luôn được tạo bằng cách gắn vào một Service ĐANG ĐƯỢC THEO DÕI (Service.ReplaceOptions).
    /// Khi EF phát hiện một entity con mới qua navigation, nó dựa vào "khoá đã được gán hay chưa" để
    /// quyết định Added hay Modified. Gán sẵn Guid.NewGuid() ở đây khiến EF tưởng đây là bản ghi cũ,
    /// phát câu UPDATE cho hàng chưa hề tồn tại, 0 dòng bị ảnh hưởng → DbUpdateConcurrencyException
    /// và người dùng nhận lỗi 500 mỗi lần cập nhật dịch vụ có tuỳ chọn.
    ///
    /// Id chỉ có giá trị thật sau SaveChanges; không nơi nào đọc nó trước lúc đó (không bảng nào tham
    /// chiếu tới ServiceOption.Id).
    /// </summary>
    public static ServiceOption Create(Guid serviceId, string name, decimal price, string unit, int sortOrder)
        => new()
        {
            ServiceId = serviceId,
            Name = name,
            Price = price,
            Unit = string.IsNullOrWhiteSpace(unit) ? "Răng" : unit.Trim(),
            SortOrder = sortOrder,
        };

    public void Update(string name, decimal price, string unit, int sortOrder)
    {
        Name = name;
        Price = price;
        Unit = string.IsNullOrWhiteSpace(unit) ? "Răng" : unit.Trim();
        SortOrder = sortOrder;
    }
}
