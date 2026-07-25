using System;
using System.Threading;
using System.Threading.Tasks;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using DentalClinic.API.Domain.Constants;

namespace DentalClinic.API.Application.UseCases.Auth;

public record ChangePasswordCommand(
    Guid UserId,
    string CurrentPassword,
    string NewPassword);

public class ChangePasswordHandler(IUserRepository userRepository, IActivityLogService activityLogService, ICurrentUserService currentUser)
{
    public async Task HandleAsync(ChangePasswordCommand command, CancellationToken ct = default)
    {
        var user = await userRepository.GetByIdAsync(command.UserId, ct)
            ?? throw new NotFoundException("Không tìm thấy tài khoản.");

        if (user.PasswordHash is null || !BCrypt.Net.BCrypt.Verify(command.CurrentPassword, user.PasswordHash))
            throw new UnauthorizedAccessException("Mật khẩu hiện tại không chính xác.");

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
