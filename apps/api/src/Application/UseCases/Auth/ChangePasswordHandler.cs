using System;
using System.Threading;
using System.Threading.Tasks;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using DentalClinic.API.Domain.Constants;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Auth;

public record ChangePasswordCommand(
    Guid UserId,
    string? CurrentPassword,
    string NewPassword) : IRequest;

public class ChangePasswordHandler(IUserRepository userRepository, IActivityLogService activityLogService, ICurrentUserService currentUser) : IRequestHandler<ChangePasswordCommand>
{
    public async Task Handle(ChangePasswordCommand command, CancellationToken ct)
    {
        var user = await userRepository.GetByIdAsync(command.UserId, ct)
            ?? throw new NotFoundException("Không tìm thấy tài khoản.");

        // Nếu là đăng nhập lần đầu (MustChangePassword == true) hoặc tài khoản Google (PasswordHash == null)
        // thì không cần mật khẩu hiện tại.
        // Đối với đổi mật khẩu thông thường trong app, bắt buộc phải nhập và kiểm tra đúng mật khẩu hiện tại.
        var isFirstTimeOrGoogle = user.MustChangePassword || user.PasswordHash is null;

        if (!isFirstTimeOrGoogle)
        {
            if (string.IsNullOrWhiteSpace(command.CurrentPassword))
            {
                throw new ValidationException("Mật khẩu hiện tại không được để trống.");
            }

            if (!BCrypt.Net.BCrypt.Verify(command.CurrentPassword, user.PasswordHash))
            {
                throw new ValidationException("Mật khẩu hiện tại không chính xác.");
            }
        }

        var newPasswordHash = BCrypt.Net.BCrypt.HashPassword(command.NewPassword, workFactor: 12);
        user.ResetPassword(newPasswordHash);

        await userRepository.UpdateAsync(user, ct);

        await activityLogService.LogAsync(
            userId: currentUser.UserId,
            userName: currentUser.UserName,
            userRole: currentUser.UserRole,
            action: ActivityAction.Edit,
            module: ActivityModule.Account,
            description: "Đổi mật khẩu tài khoản",
            status: ActivityStatus.Success,
            ipAddress: currentUser.IpAddress,
            targetId: command.UserId.ToString(),
            ct: ct);
    }
}
