using DentalClinic.API.Application.UseCases.Queue;
using FluentValidation;

namespace DentalClinic.API.Application.Validators.Queue;

public class ReorderQueuePatientCommandValidator : AbstractValidator<ReorderQueuePatientCommand>
{
    public ReorderQueuePatientCommandValidator()
    {
        RuleFor(x => x.AppointmentId).NotEmpty().WithMessage("Thiếu lịch hẹn.");
        RuleFor(x => x.SwapWithAppointmentId).NotEmpty().WithMessage("Thiếu lịch hẹn cần đổi chỗ.");
        RuleFor(x => x)
            .Must(x => x.AppointmentId != x.SwapWithAppointmentId)
            .WithMessage("Không thể đổi chỗ với chính bệnh nhân đó.")
            .OverridePropertyName(nameof(ReorderQueuePatientCommand.SwapWithAppointmentId));
    }
}
