namespace DentalClinic.API.Domain.Interfaces.Services;

/// <summary>
/// Gửi SMS tới số điện thoại Việt Nam.
///
/// Tách riêng khỏi <see cref="IEmailService"/> vì hai kênh phục vụ hai loại nội dung khác nhau:
/// SMS cho thứ NGẮN và CẦN NGAY (mã xác thực, nhắc lịch), email cho thứ DÀI và CẦN LƯU LẠI
/// (hóa đơn, đơn thuốc, chi tiết lịch hẹn).
/// </summary>
public interface ISmsService
{
    /// <summary>
    /// Gửi một tin nhắn tới một số. Ném <see cref="InvalidOperationException"/> nếu chưa cấu hình
    /// nhà cung cấp SMS ở môi trường khác Development — im lặng bỏ qua sẽ khiến người dùng chờ mã
    /// không bao giờ tới.
    /// </summary>
    /// <param name="phoneNumber">Số điện thoại người nhận, dạng 0xxxxxxxxx hoặc +84xxxxxxxxx.</param>
    /// <param name="content">
    /// Nội dung. NÊN VIẾT KHÔNG DẤU: tin có dấu tiếng Việt chỉ chứa 70 ký tự/tin thay vì 160,
    /// nên một câu bình thường dễ bị tính thành 2–3 tin, đội chi phí gấp đôi gấp ba.
    /// </param>
    Task SendAsync(string phoneNumber, string content, CancellationToken ct = default);
}
