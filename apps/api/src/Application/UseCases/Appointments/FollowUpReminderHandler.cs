using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Application.UseCases.Appointments;

public record SetFollowUpReminderRequest(DateOnly FollowUpDate, string? Note);

public class FollowUpReminderDto
{
    public Guid AppointmentId { get; set; }
    public DateOnly? FollowUpDate { get; set; }
    public string? FollowUpNote { get; set; }
}

/// <summary>
/// Nhắc tái khám: bác sĩ chỉ hẹn ngày khám lại (không đặt lịch mới).
/// Khi bác sĩ kết thúc điều trị, hệ thống gửi thông báo cho bệnh nhân (xem UpdateAppointmentStatusHandler).
/// </summary>
public class FollowUpReminderHandler(AppDbContext dbContext)
{
    public async Task<FollowUpReminderDto> SetAsync(Guid appointmentId, SetFollowUpReminderRequest request, CancellationToken ct = default)
    {
        var appointment = await dbContext.Appointments
            .FirstOrDefaultAsync(a => a.Id == appointmentId, ct)
            ?? throw new NotFoundException("Không tìm thấy lịch hẹn.");

        if (appointment.Status != AppointmentStatus.InProgress)
            throw new ValidationException("Chỉ có thể hẹn tái khám khi buổi hẹn đang trong trạng thái đang khám.");

        if (request.FollowUpDate <= DateOnly.FromDateTime(DateTime.Today))
            throw new ValidationException("Ngày tái khám phải sau ngày hôm nay.");

        appointment.SetFollowUpReminder(request.FollowUpDate, string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim());
        await dbContext.SaveChangesAsync(ct);

        return ToDto(appointmentId, appointment.FollowUpDate, appointment.FollowUpNote);
    }

    public async Task<FollowUpReminderDto> ClearAsync(Guid appointmentId, CancellationToken ct = default)
    {
        var appointment = await dbContext.Appointments
            .FirstOrDefaultAsync(a => a.Id == appointmentId, ct)
            ?? throw new NotFoundException("Không tìm thấy lịch hẹn.");

        appointment.SetFollowUpReminder(null, null);
        await dbContext.SaveChangesAsync(ct);

        return ToDto(appointmentId, null, null);
    }

    private static FollowUpReminderDto ToDto(Guid appointmentId, DateOnly? date, string? note) => new()
    {
        AppointmentId = appointmentId,
        FollowUpDate = date,
        FollowUpNote = note
    };
}
