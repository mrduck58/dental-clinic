namespace DentalClinic.API.Application.DTOs.Auth;

/// <param name="MustChangePassword">
/// Tài khoản đang dùng mật khẩu tạm do phòng khám cấp qua email — client phải đưa thẳng người dùng
/// vào màn đổi mật khẩu. Thêm ở CUỐI record để bản app cũ (đọc theo tên trường) không vỡ.
/// </param>
public record LoginResponseDto(
    string AccessToken,
    int ExpiresIn,
    AuthUserDto User,
    bool MustChangePassword = false);

public record AuthUserDto(
    Guid Id,
    string Username,
    string? FullName,
    string Email,
    string Role,
    bool IsActive,
    string? ProfilePictureUrl);
