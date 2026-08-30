using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Reminders;

public record SetFollowUpReminderCommand(Guid AppointmentId, SetFollowUpReminderRequest Request)
    : IRequest<FollowUpReminderDto>;

public record ClearFollowUpReminderCommand(Guid AppointmentId) : IRequest<FollowUpReminderDto>;

public record GetFollowUpDueQuery : IRequest<List<FollowUpDueDto>>;

public class SetFollowUpReminderHandler(
    IAppointmentRepository appointmentRepository,
    IFollowUpRepository followUpRepository)
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

        var note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();
        appointment.SetFollowUpReminder(request.FollowUpDate, note);
        await appointmentRepository.UpdateAsync(appointment, ct);

        // Tạo hoặc cập nhật thực thể FollowUp độc lập
        var followUp = FollowUp.Create(
            appointment.PatientId,
            appointment.DentistId,
            appointment.Id,
            request.FollowUpDate,
            note,
            request.TreatmentPlanItemId,
            request.TreatmentSessionId);

        await followUpRepository.AddAsync(followUp, ct);

        return FollowUpReminderMapper.ToDto(appointmentId, appointment.FollowUpDate, appointment.FollowUpNote, followUp.Id);
    }
}

public class ClearFollowUpReminderHandler(
    IAppointmentRepository appointmentRepository,
    IFollowUpRepository followUpRepository)
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

public class GetFollowUpDueHandler(
    IAppointmentRepository appointmentRepository,
    ITreatmentPlanRepository treatmentPlanRepository) : IRequestHandler<GetFollowUpDueQuery, List<FollowUpDueDto>>
{
    public async Task<List<FollowUpDueDto>> Handle(GetFollowUpDueQuery request, CancellationToken ct)
    {
        var scheduled = await appointmentRepository.GetFollowUpScheduledAsync(ct);
        if (scheduled.Count == 0) return new List<FollowUpDueDto>();

        var scheduledIds = scheduled.Select(a => a.Id).ToList();
        var checkedInSet = await appointmentRepository.GetCheckedInFollowUpOriginalIdsAsync(scheduledIds, ct);
        var patientIds = scheduled.Select(a => a.PatientId).Distinct().ToList();
        var activePlans = await treatmentPlanRepository.GetActiveByPatientIdsAsync(patientIds, ct);
        var parentById = await appointmentRepository.GetFollowUpParentMapAsync(patientIds, ct);

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
            var activeServicePlan = plansInChain.FirstOrDefault();

            result.Add(new FollowUpDueDto
            {
                OriginalAppointmentId = a.Id,
                FollowUpId = a.FollowUpId,
                PatientId = a.PatientId,
                PatientName = a.Patient.FullName,
                PatientPhone = a.Patient.PhoneNumber ?? a.Patient.User?.PhoneNumber,
                PatientDateOfBirth = a.Patient.DateOfBirth,
                Gender = a.Patient.Gender,
                DentistId = a.DentistId,
                DentistName = a.Dentist.FullName,
                ServiceId = a.ServiceId,
                ServiceName = a.Service?.Name,
                PrefillServiceId = activeServicePlan?.ServiceId ?? a.ServiceId,
                OriginalAppointmentDate = a.AppointmentDate,
                FollowUpDate = a.FollowUpDate,
                FollowUpNote = a.FollowUpNote,
                ActivePlans = planNames
            });
        }

        return result.OrderBy(x => x.FollowUpDate ?? DateOnly.MaxValue).ToList();
    }
}
