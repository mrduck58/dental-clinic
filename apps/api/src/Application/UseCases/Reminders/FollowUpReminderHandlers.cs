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
            var plansInChain = activePlans.Where(p => chain.Contains(p.AppointmentId)).ToList();
            var planNames = plansInChain.Select(p => p.ServiceName).Distinct().ToList();

            // Dịch vụ điền sẵn khi staff check-in phải là dịch vụ ĐANG ĐIỀU TRỊ (liệu trình InProgress
            // trong cùng chuỗi tái khám), không phải dịch vụ đặt lúc đầu — bệnh nhân tái khám thường đến
            // vì liệu trình đang làm dở, có thể khác hẳn dịch vụ đã chọn ở buổi đặt lịch ban đầu. Không
            // còn liệu trình nào đang thực hiện (ví dụ hẹn tái khám tay, chưa lập liệu trình) thì mới
            // dùng tạm dịch vụ của buổi hẹn gốc.
            var activeServicePlan = plansInChain.FirstOrDefault();

            result.Add(new FollowUpDueDto
            {
                OriginalAppointmentId = a.Id,
                PatientId = a.PatientId,
                PatientName = a.Patient.FullName,
                PatientPhone = a.Patient.PhoneNumber ?? a.Patient.User?.PhoneNumber,
                PatientDateOfBirth = a.Patient.DateOfBirth,
                Gender = a.Patient.Gender,
                DentistId = a.DentistId,
                DentistName = a.Dentist.FullName,
                ServiceId = activeServicePlan?.ServiceId ?? a.ServiceId,
                ServiceName = activeServicePlan?.ServiceName ?? a.Service?.Name,
                OriginalAppointmentDate = a.AppointmentDate,
                FollowUpDate = a.FollowUpDate,
                FollowUpNote = a.FollowUpNote,
                ActivePlans = planNames
            });
        }

        return result.OrderBy(x => x.FollowUpDate ?? DateOnly.MaxValue).ToList();
    }
}
