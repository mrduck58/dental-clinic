namespace DentalClinic.API.Domain.Enums;

/// <summary>
/// Danh mục chi phí tự nhập tay. "Vật tư" (SupplyTransaction) và "Lương" (PayrollRecord) đã có
/// nguồn dữ liệu riêng của chúng nên không lặp lại ở đây — trang Chi phí gộp cả hai vào báo cáo
/// tổng hợp nhưng không cho sửa/xoá qua domain này.
/// </summary>
public enum ExpenseCategory
{
    Medicine,
    Equipment,
    Rent,
    Utilities,
    Marketing,
    Maintenance,
    Software,
    Other,
}

public enum RecurrenceFrequency
{
    Monthly,
    Quarterly,
    Yearly,
}
