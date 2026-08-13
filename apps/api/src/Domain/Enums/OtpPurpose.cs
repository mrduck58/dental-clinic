namespace DentalClinic.API.Domain.Enums;

public enum OtpPurpose
{
    /// <summary>
    /// CŨ — xác thực tài khoản sau khi bệnh nhân tự đăng ký. Luồng tự đăng ký đã bỏ; giữ lại giá trị
    /// để EF đọc được các bản ghi cũ còn trong bảng.
    /// </summary>
    Registration,

    /// <summary>Quên mật khẩu (bệnh nhân).</summary>
    PasswordReset,

    /// <summary>
    /// Xác thực địa chỉ email TRƯỚC KHI lễ tân cấp tài khoản cho bệnh nhân. Không có bước này thì
    /// lễ tân gõ nhầm một ký tự là mật khẩu bay tới hộp thư người lạ — và người đó có ngay thông tin
    /// đăng nhập vào hồ sơ bệnh án của bệnh nhân thật.
    /// </summary>
    PatientAccountEmail,
}
