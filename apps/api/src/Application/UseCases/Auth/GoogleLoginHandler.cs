using DentalClinic.API.Application.DTOs.Auth;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Auth;

public record GoogleLoginCommand(string IdToken) : IRequest<GoogleLoginResponseDto>;

public class GoogleLoginHandler(
    IGoogleAuthService googleAuthService,
    IUserRepository userRepository,
    IJwtService jwtService) : IRequestHandler<GoogleLoginCommand, GoogleLoginResponseDto>
{
    public async Task<GoogleLoginResponseDto> Handle(GoogleLoginCommand command, CancellationToken ct)
    {
        var googleUser = await googleAuthService.VerifyIdTokenAsync(command.IdToken, ct);

        // Google là PHƯƠNG THỨC ĐĂNG NHẬP, không phải phương thức đăng ký. Trước đây lần đăng nhập
        // đầu tiên tự tạo luôn tài khoản — không OTP, không xác minh gì — nên đây là đường lập tài
        // khoản hàng loạt dễ hơn cả /auth/register, và nó vô hiệu hóa việc để lễ tân gác cửa.
        // Tài khoản tạo bằng Google từ trước vẫn đăng nhập bình thường (họ không có mật khẩu,
        // chặn ở đây là khóa cứng họ ra ngoài).
        var user = await userRepository.GetByEmailAsync(googleUser.Email, ct)
            ?? throw new UnauthorizedAccessException(
                "Chưa có tài khoản dùng email này. Vui lòng liên hệ phòng khám để được tạo tài khoản.");

        if (user.Role != UserRole.Patient)
        {
            throw new UnauthorizedAccessException("Tài khoản này không thể đăng nhập bằng ứng dụng di động.");
        }

        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException("Tài khoản đã bị vô hiệu hóa. Vui lòng liên hệ quản trị viên.");
        }

        var token = jwtService.GenerateToken(user);

        var profilePic = user.Role == UserRole.Patient
            ? user.Patient?.ProfilePictureUrl
            : user.Employee?.ProfilePictureUrl;

        return new GoogleLoginResponseDto(
            AccessToken: token,
            ExpiresIn: 15 * 60,
            // Luôn false: Google không còn tạo tài khoản mới. Giữ lại trường này thay vì bỏ khỏi DTO
            // vì bản app đang chạy đọc json['isNewUser'] và sẽ nổ nếu thiếu.
            IsNewUser: false,
            User: new AuthUserDto(user.Id, user.Username ?? user.Email, user.FullName, user.Email, user.Role.ToString(), user.IsActive, profilePic));
    }
}
