namespace DentalClinic.API.Infrastructure.Settings;

public record EmailSettings
{
    /// <summary>
    /// API key của Brevo (dạng "xkeysib-..."). Nạp qua biến môi trường EmailSettings__ApiKey,
    /// KHÔNG commit.
    ///
    /// Dùng HTTP API thay vì SMTP vì Render chặn cổng SMTP 25/465/587 — SMTP không bao giờ gửi
    /// được ở môi trường thật, còn Brevo đi qua cổng 443 nên chạy ở mọi nơi.
    /// </summary>
    public string ApiKey { get; init; } = string.Empty;

    /// <summary>Địa chỉ gửi. Phải là địa chỉ ĐÃ XÁC THỰC trong Brevo, nếu không Brevo trả 4xx.</summary>
    public string FromEmail { get; init; } = string.Empty;

    public string FromName { get; init; } = "Dental Clinic System";
    public string ClinicName { get; init; } = "Sơn Giang Dental";

    /// <summary>
    /// Chưa cấu hình thì ở Development chỉ ghi log nội dung thư (tiện thử, không cần tài khoản),
    /// còn ngoài Development sẽ ném lỗi thay vì âm thầm bỏ qua.
    /// </summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ApiKey) && !string.IsNullOrWhiteSpace(FromEmail);
}
