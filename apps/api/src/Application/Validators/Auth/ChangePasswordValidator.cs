using DentalClinic.API.Application.DTOs.Auth;
using FluentValidation;

namespace DentalClinic.API.Application.Validators.Auth;

public class ChangePasswordValidator : AbstractValidator<ChangePasswordRequestDto>
{
    public ChangePasswordValidator()
    {
        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Mật khẩu mới không được để trống.")
            .MinimumLength(8).WithMessage("Mật khẩu mới phải có ít nhất 8 ký tự.")
            .Must((dto, newPass) => string.IsNullOrWhiteSpace(dto.CurrentPassword) || dto.CurrentPassword != newPass)
            .WithMessage("Mật khẩu mới không được trùng với mật khẩu hiện tại.");
    }
}
