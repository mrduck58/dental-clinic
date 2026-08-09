using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Patients;

public record RequestPatientEmailVerificationCommand(string Email) : IRequest;

/// <summary>
/// Gửi mã xác thực tới email mà lễ tân vừa nhập, TRƯỚC KHI cấp tài khoản. Bệnh nhân mở hộp thư,
/// đọc mã cho lễ tân nhập lại.
///
/// Không có bước này thì lễ tân gõ nhầm một ký tự (gmial.com) là mật khẩu bay tới hộp thư người lạ —
/// và người đó có ngay thông tin đăng nhập vào hồ sơ bệnh án của bệnh nhân thật. Đó là rò rỉ dữ liệu
/// y tế, không chỉ là chuyện bệnh nhân không nhận được thư.
/// </summary>
public class RequestPatientEmailVerificationHandler(
    IUserRepository userRepository,
    IOtpRepository otpRepository,
    IEmailService emailService) : IRequestHandler<RequestPatientEmailVerificationCommand>
{
    public async Task Handle(RequestPatientEmailVerificationCommand command, CancellationToken ct)
    {
        var email = command.Email.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            throw new ValidationException("Địa chỉ email không hợp lệ.");

        // Chặn sớm ở đây thay vì để lễ tân đọc mã xong mới báo lỗi ở bước tạo tài khoản.
        if (await userRepository.ExistsByEmailAsync(email, ct))
            throw new ConflictException($"Email '{email}' đã có tài khoản. Hãy tra cứu bệnh nhân thay vì tạo mới.");

        // Bỏ hiệu lực các mã cũ của cùng email: gửi mã mới mà mã cũ vẫn dùng được thì mỗi lần bấm
        // "gửi lại" lại nhân thêm một mã hợp lệ, tăng số cửa để dò.
        await otpRepository.InvalidateAllAsync(email, OtpPurpose.PatientAccountEmail, ct);

        var otp = OtpCode.Create(email, OtpPurpose.PatientAccountEmail);
        await otpRepository.AddAsync(otp, ct);

        await emailService.SendPatientAccountVerificationAsync(email, otp.Code, ct);
    }
}
