using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Auth;

public record ToggleAccountStatusCommand(Guid Id) : IRequest<AccountDto>;

/// <summary>Bật/tắt quyền đăng nhập của 1 tài khoản (trang Tài khoản &amp; Phân quyền — Admin/Owner).
/// Chỉ đổi <see cref="Domain.Entities.User.IsActive"/> (quyền đăng nhập hệ thống), KHÔNG đụng tới
/// trạng thái nhân sự (Staff.EmploymentStatus) — 2 khái niệm khác nhau.</summary>
public class ToggleAccountStatusHandler(IUserRepository userRepository) : IRequestHandler<ToggleAccountStatusCommand, AccountDto>
{
    public async Task<AccountDto> Handle(ToggleAccountStatusCommand command, CancellationToken ct)
    {
        var id = command.Id;
        var user = await userRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Không tìm thấy tài khoản với ID: {id}");

        user.SetActive(!user.IsActive);
        await userRepository.UpdateAsync(user, ct);

        return new AccountDto(
            user.Id, user.Username!, user.FullName, user.Email,
            user.PhoneNumber, user.Role.ToString(), user.IsActive, user.CreatedAt);
    }
}
