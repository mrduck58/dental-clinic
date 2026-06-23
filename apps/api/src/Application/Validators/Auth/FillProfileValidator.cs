using DentalClinic.API.Application.DTOs.Auth;
using FluentValidation;

namespace DentalClinic.API.Application.Validators.Auth;

public class FillProfileValidator : AbstractValidator<FillProfileRequestDto>
{
    public FillProfileValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().When(x => string.IsNullOrEmpty(x.FullName)).WithMessage("Tên không được để trống.")
            .MaximumLength(100).WithMessage("Tên không được vượt quá 100 ký tự.")
            .Matches(@"^[\p{L}\s]+$").When(x => !string.IsNullOrEmpty(x.FirstName)).WithMessage("Tên chỉ được chứa chữ cái và khoảng trắng.");

        RuleFor(x => x.LastName)
            .NotEmpty().When(x => string.IsNullOrEmpty(x.FullName)).WithMessage("Họ không được để trống.")
            .MaximumLength(100).WithMessage("Họ không được vượt quá 100 ký tự.")
            .Matches(@"^[\p{L}\s]+$").When(x => !string.IsNullOrEmpty(x.LastName)).WithMessage("Họ chỉ được chứa chữ cái và khoảng trắng.");

        RuleFor(x => x.FullName)
            .NotEmpty().When(x => string.IsNullOrEmpty(x.FirstName) && string.IsNullOrEmpty(x.LastName)).WithMessage("Họ và tên không được để trống.")
            .MaximumLength(200).WithMessage("Họ và tên không được vượt quá 200 ký tự.");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Số điện thoại không được để trống.")
            .Matches(@"^(0[3|5|7|8|9])+([0-9]{8})$")
            .WithMessage("Số điện thoại không hợp lệ (số Việt Nam 10 chữ số, bắt đầu bằng 03/05/07/08/09).");

        RuleFor(x => x.DateOfBirth)
            .Must(d => d == null || d.Value <= DateOnly.FromDateTime(DateTime.Today.AddYears(-1)))
            .WithMessage("Ngày sinh không hợp lệ.")
            .Must(d => d == null || d.Value >= DateOnly.FromDateTime(DateTime.Today.AddYears(-120)))
            .WithMessage("Ngày sinh không hợp lệ.");

        RuleFor(x => x.Gender)
            .Must(g => g == null || g == "Nam" || g == "Nữ" || g == "Khác")
            .WithMessage("Giới tính không hợp lệ.");
    }
}
