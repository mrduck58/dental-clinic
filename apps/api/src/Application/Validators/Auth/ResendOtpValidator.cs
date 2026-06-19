using DentalClinic.API.Application.DTOs.Auth;
using FluentValidation;

namespace DentalClinic.API.Application.Validators.Auth;

public class ResendOtpValidator : AbstractValidator<ResendOtpRequestDto>
{
    public ResendOtpValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email không được để trống.")
            .EmailAddress().WithMessage("Email không đúng định dạng.");
    }
}
