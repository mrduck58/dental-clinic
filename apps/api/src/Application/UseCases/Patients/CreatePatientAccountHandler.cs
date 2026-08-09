using System.Security.Cryptography;
using DentalClinic.API.Domain.Constants;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Patients;

/// <param name="PhoneNumber">
/// BẮT BUỘC. Đây là khóa mà toàn hệ thống dùng để nhận ra bệnh nhân cũ (luồng đặt lịch tại quầy dò
/// theo số này). Thiếu nó thì lần sau bệnh nhân quay lại sẽ bị tạo hồ sơ mới, lịch sử khám tách đôi.
/// </param>
/// <param name="VerificationCode">
/// Mã lấy từ email, do bệnh nhân đọc lại cho lễ tân. BẮT BUỘC — không có nó thì một ký tự gõ nhầm
/// là mật khẩu bay tới hộp thư người lạ, kèm quyền vào hồ sơ bệnh án của bệnh nhân thật.
/// </param>
public record CreatePatientAccountCommand(
    string FullName,
    string Email,
    string PhoneNumber,
    DateOnly? DateOfBirth,
    string? Gender,
    string VerificationCode) : IRequest<CreatePatientAccountResult>;

/// <param name="LinkedExistingPatient">
/// true khi bệnh nhân đã có hồ sơ từ trước (đến khám tại quầy nhưng chưa có tài khoản) và lần này
/// chỉ được cấp thông tin đăng nhập — lễ tân cần biết để không đi tạo hồ sơ thứ hai.
/// </param>
public record CreatePatientAccountResult(
    Guid UserId,
    Guid PatientId,
    string Email,
    string FullName,
    bool LinkedExistingPatient);

/// <summary>
/// Lễ tân lập tài khoản cho bệnh nhân đến khám lần đầu. Đây là đường DUY NHẤT sinh tài khoản bệnh
/// nhân mới — tự đăng ký trên app đã bị bỏ vì bất kỳ ai cũng lập được hàng loạt tài khoản rồi giữ
/// kín khung giờ, và chỉ giới hạn theo tài khoản thì họ lập tài khoản mới là lách được.
///
/// Việc xác minh ở đây là con người: lễ tân đang đứng trước mặt bệnh nhân hoặc đang nói chuyện
/// điện thoại với họ, nên tài khoản được kích hoạt ngay, không cần OTP.
/// </summary>
public class CreatePatientAccountHandler(
    IUserRepository userRepository,
    IPatientRepository patientRepository,
    IOtpRepository otpRepository,
    IEmailService emailService,
    IActivityLogService activityLogService,
    ICurrentUserService currentUser) : IRequestHandler<CreatePatientAccountCommand, CreatePatientAccountResult>
{
    public async Task<CreatePatientAccountResult> Handle(CreatePatientAccountCommand command, CancellationToken ct)
    {
        var email = NormalizeEmail(command.Email);

        if (await userRepository.ExistsByEmailAsync(email, ct))
            throw new ConflictException($"Email '{email}' đã có tài khoản. Hãy tra cứu bệnh nhân thay vì tạo mới.");

        await ConsumeEmailVerificationAsync(email, command.VerificationCode, ct);

        var username = await BuildUniqueUsernameAsync(email, ct);
        var rawPassword = GenerateTemporaryPassword();
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(rawPassword, workFactor: 12);

        // Bệnh nhân có thể đã tới khám tại quầy trước đó và có hồ sơ (khóa theo số điện thoại) nhưng
        // CHƯA có tài khoản. Trường hợp đó phải NÂNG CẤP hồ sơ cũ chứ không tạo hồ sơ thứ hai —
        // nếu tạo mới thì lịch sử khám, hóa đơn, bệnh án cũ đều nằm lại ở hồ sơ kia.
        var existing = await patientRepository.GetByPhoneNumberAsync(command.PhoneNumber, ct);

        User user;
        Patient patient;
        bool linkedExisting;

        if (existing is not null)
        {
            user = await userRepository.GetByIdAsync(existing.UserId, ct)
                ?? throw new NotFoundException("Hồ sơ bệnh nhân không gắn với tài khoản nào hợp lệ.");

            if (user.HasAccount)
                throw new ConflictException(
                    $"Bệnh nhân với số {command.PhoneNumber} đã có tài khoản đăng nhập ({user.Email}).");

            user.GrantClinicAccount(username, email, passwordHash);
            user.UpdateFullName(command.FullName);
            await userRepository.UpdateAsync(user, ct);

            patient = existing;
            linkedExisting = true;
        }
        else
        {
            user = User.CreatePatientByClinic(
                username, email, passwordHash, command.FullName, command.PhoneNumber);
            await userRepository.AddAsync(user, ct);

            patient = Patient.Create(
                userId: user.Id,
                dateOfBirth: command.DateOfBirth,
                gender: command.Gender,
                phoneNumber: command.PhoneNumber);
            patient.User = user;
            await patientRepository.AddAsync(patient, ct);
            linkedExisting = false;
        }

        // Gửi SAU khi lưu: gửi trước rồi lưu hỏng thì bệnh nhân cầm mật khẩu của một tài khoản
        // không tồn tại. Ngược lại, email hỏng thì tài khoản vẫn còn và lễ tân đặt lại mật khẩu được.
        await emailService.SendStaffCredentialsAsync(email, command.FullName, rawPassword, ct);

        await activityLogService.LogAsync(
            userId: currentUser.UserId,
            userName: currentUser.UserName,
            userRole: currentUser.UserRole,
            action: ActivityAction.Create,
            module: ActivityModule.Account,
            description: linkedExisting
                ? $"Cấp tài khoản cho bệnh nhân đã có hồ sơ: {command.FullName} ({email})"
                : $"Tạo tài khoản bệnh nhân mới: {command.FullName} ({email})",
            status: ActivityStatus.Success,
            ipAddress: currentUser.IpAddress,
            targetId: user.Id.ToString(),
            ct: ct);

        return new CreatePatientAccountResult(user.Id, patient.Id, email, command.FullName, linkedExisting);
    }

    /// <summary>
    /// Khẳng định địa chỉ email này đã được chính bệnh nhân xác nhận (họ mở hộp thư, đọc mã cho lễ tân),
    /// rồi tiêu mã đi để không dùng lại được.
    ///
    /// Đếm số lần nhập sai: mã 6 chữ số mà cho thử không giới hạn thì chỉ là khóa 20 bit không có chốt.
    /// </summary>
    private async Task ConsumeEmailVerificationAsync(string email, string code, CancellationToken ct)
    {
        var otp = await otpRepository.GetLatestValidAsync(email, OtpPurpose.PatientAccountEmail, ct)
            ?? throw new ValidationException(
                "Chưa xác thực email hoặc mã đã hết hạn. Vui lòng gửi lại mã xác thực.");

        if (otp.Code != code?.Trim())
        {
            otp.RegisterFailedAttempt();
            await otpRepository.UpdateAsync(otp, ct);

            throw new ValidationException(otp.IsUsed
                ? "Đã nhập sai mã quá số lần cho phép. Vui lòng gửi lại mã xác thực."
                : "Mã xác thực không đúng.");
        }

        otp.MarkUsed();
        await otpRepository.UpdateAsync(otp, ct);
    }

    /// <summary>
    /// Hạ chữ và bỏ khoảng trắng. Không đụng tới dấu chấm hay phần "+tag" của Gmail: giờ lễ tân là
    /// người tạo tài khoản nên không còn kịch bản tự lập hàng loạt bằng mẹo +tag, mà bỏ tag đi lại
    /// làm sai địa chỉ của người dùng thật đang cố tình phân loại thư.
    /// </summary>
    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private async Task<string> BuildUniqueUsernameAsync(string email, CancellationToken ct)
    {
        var username = email.Split('@')[0].Replace('.', '_').Replace('-', '_');

        if (await userRepository.ExistsByUsernameAsync(username, ct))
            username = $"{username}_{RandomNumberGenerator.GetHexString(4, lowercase: true)}";

        return username;
    }

    /// <summary>
    /// Mật khẩu tạm gửi qua email. Bệnh nhân bị buộc đổi ngay lần đăng nhập đầu (User.MustChangePassword)
    /// nên mật khẩu này chỉ sống tới lúc đó — nhưng vẫn sinh bằng CSPRNG chứ không phải chuỗi đoán được.
    /// </summary>
    private static string GenerateTemporaryPassword(int length = 10)
    {
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";   // bỏ I, O cho dễ đọc khi bệnh nhân gõ tay
        const string lower = "abcdefghijkmnopqrstuvwxyz";  // bỏ l
        const string digits = "23456789";                  // bỏ 0, 1
        const string special = "!@#$%&";
        const string all = upper + lower + digits + special;

        var pw = new char[length];
        var b = RandomNumberGenerator.GetBytes(length);

        pw[0] = upper[b[0] % upper.Length];
        pw[1] = lower[b[1] % lower.Length];
        pw[2] = digits[b[2] % digits.Length];
        pw[3] = special[b[3] % special.Length];
        for (var i = 4; i < length; i++) pw[i] = all[b[i] % all.Length];

        var s = RandomNumberGenerator.GetBytes(length);
        for (var i = length - 1; i > 0; i--)
        {
            var j = s[i] % (i + 1);
            (pw[i], pw[j]) = (pw[j], pw[i]);
        }

        return new string(pw);
    }
}
