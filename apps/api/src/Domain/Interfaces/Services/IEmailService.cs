namespace DentalClinic.API.Domain.Interfaces.Services;

public interface IEmailService
{
    Task SendStaffCredentialsAsync(
        string recipientEmail,
        string recipientName,
        string password,
        CancellationToken ct = default);

    // SendOtpAsync (OTP kích hoạt tài khoản) đã bỏ cùng luồng tự đăng ký. OTP đặt lại mật khẩu
    // dùng SendPasswordResetOtpAsync bên dưới, là việc khác.

    Task SendPasswordResetAsync(
        string recipientEmail,
        string recipientName,
        string resetLink,
        CancellationToken ct = default);

    /// <summary>
    /// Gửi mã xác thực để khẳng định địa chỉ email này CÓ THẬT và đúng người, trước khi phòng khám
    /// cấp tài khoản. Nếu bỏ bước này, lễ tân gõ nhầm một ký tự là mật khẩu bay tới hộp thư người lạ.
    /// </summary>
    Task SendPatientAccountVerificationAsync(
        string recipientEmail,
        string code,
        CancellationToken ct = default);

    Task SendPasswordResetOtpAsync(
        string recipientEmail,
        string code,
        CancellationToken ct = default);
}
