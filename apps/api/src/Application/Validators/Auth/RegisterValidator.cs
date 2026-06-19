using DentalClinic.API.Application.DTOs.Auth;
using FluentValidation;

namespace DentalClinic.API.Application.Validators.Auth;

public class RegisterValidator : AbstractValidator<RegisterRequestDto>
{
    public RegisterValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email không được để trống.")
            .EmailAddress().WithMessage("Email không đúng định dạng.")
            .MaximumLength(256).WithMessage("Email không được vượt quá 256 ký tự.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Mật khẩu không được để trống.")
            .MinimumLength(8).WithMessage("Mật khẩu phải có ít nhất 8 ký tự.")
            .MaximumLength(128).WithMessage("Mật khẩu không được vượt quá 128 ký tự.")
            .Matches("[A-Z]").WithMessage("Mật khẩu phải có ít nhất một chữ hoa.")
            .Matches("[a-z]").WithMessage("Mật khẩu phải có ít nhất một chữ thường.")
            .Matches("[0-9]").WithMessage("Mật khẩu phải có ít nhất một chữ số.");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("Xác nhận mật khẩu không được để trống.")
            .Equal(x => x.Password).WithMessage("Mật khẩu xác nhận không khớp.");
    }
}
