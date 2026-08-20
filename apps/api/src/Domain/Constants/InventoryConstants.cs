namespace DentalClinic.API.Domain.Constants;

public static class InventoryConstants
{
    public static readonly string[] AllowedUnits = ["Cái", "Hộp", "Tuýp", "Cuộn", "Chai", "Gói", "Bộ"];
    public static readonly string[] AllowedOrderTypes = ["standard", "custom"];

    // Danh mục vật tư — thay cho danh sách 5 nhóm cũ (Bảo hộ/Dụng cụ/Vật liệu/Tiêu hao/Thuốc), vốn chỉ là
    // nhãn hiển thị không có tác dụng nghiệp vụ. 3 nhóm này gắn trực tiếp với OrderType (xem
    // DeriveOrderType): "Vật tư chính" luôn là hàng đặt riêng cho bệnh nhân theo option dịch vụ (mão sứ,
    // veneer...), 2 nhóm còn lại luôn là hàng tồn kho dùng chung.
    public const string CategoryMain = "Vật tư chính";
    public const string CategoryConsumable = "Vật tư tiêu hao";
    public const string CategoryTechnical = "Vật tư kỹ thuật/labo";
    public static readonly string[] AllowedCategories = [CategoryMain, CategoryConsumable, CategoryTechnical];

    /// <summary>OrderType không còn cho chọn tay — suy ra thẳng từ Danh mục để tránh mâu thuẫn dữ liệu
    /// (vd lỡ chọn "Vật tư chính" nhưng lại gắn OrderType "standard").</summary>
    public static string DeriveOrderType(string category) => category == CategoryMain ? "custom" : "standard";
}
