using DentalClinic.API.Application.UseCases.Booking;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DentalClinic.API.Application.UseCases.ClinicalRecords;

public record StartTreatmentCommand(Guid AppointmentId) : IRequest;

/// <summary>Bác sĩ bắt đầu khám — tách ra từ god-handler <c>UpdateAppointmentStatusHandler</c>.</summary>
public class StartTreatmentHandler(
    IAppointmentRepository appointmentRepository,
    ILogger<StartTreatmentHandler>? logger = null) : IRequestHandler<StartTreatmentCommand>
{
    public async Task Handle(StartTreatmentCommand command, CancellationToken ct)
    {
        var appointmentId = command.AppointmentId;

        var appointment = await appointmentRepository.GetByIdAsync(appointmentId, ct);
        if (appointment == null)
        {
            logger?.LogWarning("Appointment {Id} not found for StartTreatment", appointmentId);
            throw new KeyNotFoundException($"Không tìm thấy lịch hẹn {appointmentId}.");
        }

        if (appointment.Status != AppointmentStatus.CheckedIn)
            throw new ValidationException("Chỉ có thể bắt đầu khám lịch hẹn đã check-in.");

        // Mỗi bác sĩ chỉ khám một bệnh nhân tại một thời điểm — chỉ xét trong cùng ngày với buổi hẹn này,
        // để một ca cũ quên bấm "Kết thúc điều trị" không khóa bác sĩ ở những ngày sau.
        var vnDate = TimeZoneInfo.ConvertTime(appointment.AppointmentDate, AppointmentStatusHelper.VietnamTz);
        var dayStartUtc = new DateTimeOffset(vnDate.Year, vnDate.Month, vnDate.Day, 0, 0, 0,
            AppointmentStatusHelper.VietnamTz.BaseUtcOffset).ToUniversalTime();
        var inProgress = await appointmentRepository.GetInProgressByDentistAsync(
            appointment.DentistId, appointmentId, dayStartUtc, dayStartUtc.AddDays(1), ct);
        if (inProgress != null)
            throw new ConflictException(
                $"Bạn đang khám bệnh nhân {inProgress.Patient.FullName}. Vui lòng kết thúc điều trị bệnh nhân đó trước khi bắt đầu khám bệnh nhân mới.");

        appointment.StartTreatment();
        await appointmentRepository.UpdateAsync(appointment, ct);
    }
}
