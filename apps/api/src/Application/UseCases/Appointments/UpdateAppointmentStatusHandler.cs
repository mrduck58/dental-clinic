using DentalClinic.API.Domain.Interfaces.Repositories;

namespace DentalClinic.API.Application.UseCases.Appointments;

public class UpdateAppointmentStatusHandler(IAppointmentRepository appointmentRepository)
{
    public async Task ConfirmAsync(Guid appointmentId, CancellationToken ct = default)
    {
        var appointment = await appointmentRepository.GetByIdAsync(appointmentId, ct)
            ?? throw new KeyNotFoundException($"Không tìm thấy lịch hẹn {appointmentId}.");
        appointment.Confirm();
        await appointmentRepository.UpdateAsync(appointment, ct);
    }

    public async Task CancelAsync(Guid appointmentId, CancellationToken ct = default)
    {
        var appointment = await appointmentRepository.GetByIdAsync(appointmentId, ct)
            ?? throw new KeyNotFoundException($"Không tìm thấy lịch hẹn {appointmentId}.");
        appointment.Cancel();
        await appointmentRepository.UpdateAsync(appointment, ct);
    }
}
