using DentalClinic.API.Application.DTOs.Auth;
using FluentValidation;

namespace DentalClinic.API.Application.Validators.Auth;

public class VerifyOtpValidator : AbstractValidator<VerifyOtpRequestDto>
{
    public VerifyOtpValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email không được để trống.")
            .EmailAddress().WithMessage("Email không đúng định dạng.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Mã OTP không được để trống.")
            .Length(6).WithMessage("Mã OTP phải có đúng 6 chữ số.")
            .Matches(@"^\d{6}$").WithMessage("Mã OTP chỉ được chứa chữ số.");
    }
}
