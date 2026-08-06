using DentalClinic.API.Application.UseCases.Invoices;
using FluentValidation;

namespace DentalClinic.API.Application.Validators.Invoices;

public class IssueInvoiceCommandValidator : AbstractValidator<IssueInvoiceCommand>
{
    public IssueInvoiceCommandValidator()
    {
        RuleFor(x => x.AppointmentId).NotEmpty().WithMessage("Thiếu lịch hẹn.");

        // Chỉ bắt buộc có dịch vụ ở nhánh xuất hóa đơn thông thường — nhánh thu phần còn lại
        // (ParentInvoiceId) và thu đợt liệu trình (TreatmentPlanId) tự tính số tiền theo cách khác.
        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Hóa đơn phải có ít nhất một dịch vụ.")
            .When(x => x.TreatmentPlanId is null && x.ParentInvoiceId is null);

        RuleFor(x => x.Discount).GreaterThanOrEqualTo(0).WithMessage("Số tiền giảm giá không được âm.");
        RuleFor(x => x.DepositAmount).GreaterThanOrEqualTo(0).WithMessage("Số tiền thu không được âm.");
    }
}
