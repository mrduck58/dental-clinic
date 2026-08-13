namespace DentalClinic.API.Infrastructure.Settings;

public record SmsSettings
{
    public string BaseUrl { get; init; } = "https://api.speedsms.vn/index.php/";

    /// <summary>
    /// API access token lấy ở connect.speedsms.vn. Nạp qua biến môi trường
    /// <c>SmsSettings__ApiToken</c>, KHÔNG commit vào appsettings.
    /// </summary>
    public string ApiToken { get; init; } = string.Empty;

    /// <summary>
    /// Loại tin của SpeedSMS:
    ///   2 = chăm sóc khách hàng, hiển thị đầu số ngẫu nhiên — rẻ nhất (~250–300đ/tin), không cần đăng ký gì.
    ///   3 = brandname riêng — hiện tên phòng khám, PHẢI đăng ký trước (giấy phép kinh doanh, duyệt mẫu tin).
    ///   4 = brandname dùng chung "Verify" — ~750đ/tin, KHÔNG cần thủ tục đăng ký.
    ///
    /// Mặc định 4: đắt hơn loại 2 nhưng người nhận thấy tên người gửi thay vì một đầu số lạ, mà vẫn
    /// không phải chờ vài tuần duyệt hồ sơ như loại 3.
    /// </summary>
    public int SmsType { get; init; } = 4;

    /// <summary>Bắt buộc khi <see cref="SmsType"/> = 3 — tên định danh đã đăng ký.</summary>
    public string? Sender { get; init; }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiToken);
}
