using DentalClinic.API.Application.UseCases.Queue;
using FluentValidation;

namespace DentalClinic.API.Application.Validators.Queue;

public class TransferQueuePatientCommandValidator : AbstractValidator<TransferQueuePatientCommand>
{
    public TransferQueuePatientCommandValidator()
    {
        RuleFor(x => x.AppointmentId).NotEmpty().WithMessage("Thiếu lịch hẹn.");
        RuleFor(x => x.RoomName).NotEmpty().WithMessage("Thiếu tên phòng đích.");
    }
}
