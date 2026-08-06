using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Reminders;

// ── Command/Query ────────────────────────────────────────────────────────────

public record SetFollowUpReminderCommand(Guid AppointmentId, SetFollowUpReminderRequest Request)
    : IRequest<FollowUpReminderDto>;

public record ClearFollowUpReminderCommand(Guid AppointmentId) : IRequest<FollowUpReminderDto>;

public record GetFollowUpDueQuery : IRequest<List<FollowUpDueDto>>;

public record CheckInFollowUpCommand(Guid OriginalAppointmentId) : IRequest<Guid>;

// ── Handlers ─────────────────────────────────────────────────────────────────
// Nhắc tái khám: bác sĩ chỉ hẹn ngày khám lại (không đặt lịch mới). Khi bác sĩ kết thúc điều trị,
// hệ thống gửi thông báo cho bệnh nhân (xem ClinicalRecords/EndTreatmentHandler).
// Tách ra từ god-handler FollowUpReminderHandler (4 method).

public class SetFollowUpReminderHandler(IAppointmentRepository appointmentRepository)
    : IRequestHandler<SetFollowUpReminderCommand, FollowUpReminderDto>
{
    public async Task<FollowUpReminderDto> Handle(SetFollowUpReminderCommand command, CancellationToken ct)
    {
        var appointmentId = command.AppointmentId;
        var request = command.Request;

        var appointment = await appointmentRepository.GetByIdAsync(appointmentId, ct)
            ?? throw new NotFoundException("Không tìm thấy lịch hẹn.");

        if (appointment.Status is not (AppointmentStatus.InProgress or AppointmentStatus.PendingPayment or AppointmentStatus.Completed))
            throw new ValidationException("Chỉ có thể hẹn tái khám khi buổi hẹn đang khám hoặc đã kết thúc điều trị.");

        if (request.FollowUpDate <= DateOnly.FromDateTime(DateTime.Today))
            throw new ValidationException("Ngày tái khám phải sau ngày hôm nay.");

        appointment.SetFollowUpReminder(request.FollowUpDate, string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim());
        await appointmentRepository.UpdateAsync(appointment, ct);

        return FollowUpReminderMapper.ToDto(appointmentId, appointment.FollowUpDate, appointment.FollowUpNote);
    }
}

public class ClearFollowUpReminderHandler(IAppointmentRepository appointmentRepository)
    : IRequestHandler<ClearFollowUpReminderCommand, FollowUpReminderDto>
{
    public async Task<FollowUpReminderDto> Handle(ClearFollowUpReminderCommand command, CancellationToken ct)
    {
        var appointmentId = command.AppointmentId;

        var appointment = await appointmentRepository.GetByIdAsync(appointmentId, ct)
            ?? throw new NotFoundException("Không tìm thấy lịch hẹn.");

        appointment.SetFollowUpReminder(null, null);
        await appointmentRepository.UpdateAsync(appointment, ct);

        return FollowUpReminderMapper.ToDto(appointmentId, null, null);
    }
}

/// <summary>
/// Danh sách bệnh nhân đang chờ tái khám: các buổi hẹn đã kết thúc điều trị mà BÁC SĨ
/// đã hẹn ngày tái khám (FollowUpDate) ở tab Tái khám. Bệnh nhân không cần đặt lịch lại —
/// staff check-in trực tiếp từ danh sách này.
/// Buổi gốc đã được check-in tái khám (có buổi con chưa hủy) sẽ được ẩn để tránh trùng.
/// </summary>
public class GetFollowUpDueHandler(
    IAppointmentRepository appointmentRepository,
    ITreatmentPlanRepository treatmentPlanRepository) : IRequestHandler<GetFollowUpDueQuery, List<FollowUpDueDto>>
{
    public async Task<List<FollowUpDueDto>> Handle(GetFollowUpDueQuery request, CancellationToken ct)
    {
        // Buổi hẹn đã kết thúc điều trị và được bác sĩ hẹn tái khám.
        var scheduled = await appointmentRepository.GetFollowUpScheduledAsync(ct);

        if (scheduled.Count == 0) return new List<FollowUpDueDto>();

        var scheduledIds = scheduled.Select(a => a.Id).ToList();

        // Buổi gốc đã được check-in tái khám (buổi con chưa hủy) → ẩn.
        var checkedInSet = await appointmentRepository.GetCheckedInFollowUpOriginalIdsAsync(scheduledIds, ct);

        var patientIds = scheduled.Select(a => a.PatientId).Distinct().ToList();

        // Liệu trình đang thực hiện (để hiển thị bối cảnh "đang điều trị", có thể rỗng).
        var activePlans = await treatmentPlanRepository.GetActiveByPatientIdsAsync(patientIds, ct);

        // Bản đồ cha-con để gom liệu trình theo đúng chuỗi tái khám của mỗi buổi.
        var parentById = await appointmentRepository.GetFollowUpParentMapAsync(patientIds, ct);

        // Chuỗi tái khám của một buổi hẹn: chính nó + các buổi gốc phía trên (chặn vòng lặp).
        HashSet<Guid> ChainOf(Guid id)
        {
            var chain = new HashSet<Guid>();
            Guid? cursor = id;
            while (cursor is Guid c && chain.Add(c))
                cursor = parentById.TryGetValue(c, out var next) ? next : null;
            return chain;
        }

        var result = new List<FollowUpDueDto>();
        foreach (var a in scheduled)
        {
            if (checkedInSet.Contains(a.Id)) continue;

            var chain = ChainOf(a.Id);
            var planNames = activePlans
                .Where(p => chain.Contains(p.AppointmentId))
                .Select(p => p.ServiceName)
                .Distinct()
                .ToList();

            result.Add(new FollowUpDueDto
            {
                OriginalAppointmentId = a.Id,
                PatientId = a.PatientId,
                PatientName = a.Patient.FullName,
                PatientPhone = a.Patient.PhoneNumber ?? a.Patient.User?.PhoneNumber,
                Gender = a.Patient.Gender,
                DentistName = a.Dentist.FullName,
                ServiceName = a.Service?.Name,
                OriginalAppointmentDate = a.AppointmentDate,
                FollowUpDate = a.FollowUpDate,
                FollowUpNote = a.FollowUpNote,
                ActivePlans = planNames
            });
        }

        return result.OrderBy(x => x.FollowUpDate ?? DateOnly.MaxValue).ToList();
    }
}

/// <summary>
/// Staff check-in bệnh nhân đến tái khám: tạo buổi hẹn mới đã check-in ngay,
/// gắn về buổi gốc — bác sĩ sẽ thấy cờ tái khám và liệu trình cũ của bệnh nhân.
/// </summary>
public class CheckInFollowUpHandler(IAppointmentRepository appointmentRepository) : IRequestHandler<CheckInFollowUpCommand, Guid>
{
    public async Task<Guid> Handle(CheckInFollowUpCommand command, CancellationToken ct)
    {
        var originalAppointmentId = command.OriginalAppointmentId;

        var original = await appointmentRepository.GetByIdAsync(originalAppointmentId, ct)
            ?? throw new NotFoundException("Không tìm thấy buổi hẹn gốc.");

        // Chỉ check-in tái khám được khi bác sĩ đã hẹn ngày tái khám cho buổi này.
        if (original.FollowUpDate == null)
            throw new ValidationException("Buổi hẹn này chưa được bác sĩ hẹn tái khám.");

        // Chặn check-in tái khám lặp cho cùng một buổi gốc (buổi hủy không tính).
        // Các lịch hẹn/lượt khám khác của bệnh nhân là lần khám riêng — không ảnh hưởng.
        var alreadyCheckedIn = await appointmentRepository.HasActiveFollowUpCheckInAsync(originalAppointmentId, ct);
        if (alreadyCheckedIn)
            throw new ConflictException("Buổi hẹn này đã được check-in tái khám.");

        var followUpVisit = Appointment.CheckInFollowUp(
            original.Id,
            original.PatientId,
            original.DentistId,
            original.ServiceId,
            string.IsNullOrWhiteSpace(original.FollowUpNote) ? "Tái khám theo hẹn" : $"Tái khám: {original.FollowUpNote}");

        await appointmentRepository.AddAsync(followUpVisit, ct);

        return followUpVisit.Id;
    }
}
