using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

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

public class SetFollowUpReminderHandler(AppDbContext dbContext)
    : IRequestHandler<SetFollowUpReminderCommand, FollowUpReminderDto>
{
    public async Task<FollowUpReminderDto> Handle(SetFollowUpReminderCommand command, CancellationToken ct)
    {
        var appointmentId = command.AppointmentId;
        var request = command.Request;

        var appointment = await dbContext.Appointments
            .FirstOrDefaultAsync(a => a.Id == appointmentId, ct)
            ?? throw new NotFoundException("Không tìm thấy lịch hẹn.");

        if (appointment.Status is not (AppointmentStatus.InProgress or AppointmentStatus.PendingPayment or AppointmentStatus.Completed))
            throw new ValidationException("Chỉ có thể hẹn tái khám khi buổi hẹn đang khám hoặc đã kết thúc điều trị.");

        if (request.FollowUpDate <= DateOnly.FromDateTime(DateTime.Today))
            throw new ValidationException("Ngày tái khám phải sau ngày hôm nay.");

        appointment.SetFollowUpReminder(request.FollowUpDate, string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim());
        await dbContext.SaveChangesAsync(ct);

        return FollowUpReminderMapper.ToDto(appointmentId, appointment.FollowUpDate, appointment.FollowUpNote);
    }
}

public class ClearFollowUpReminderHandler(AppDbContext dbContext)
    : IRequestHandler<ClearFollowUpReminderCommand, FollowUpReminderDto>
{
    public async Task<FollowUpReminderDto> Handle(ClearFollowUpReminderCommand command, CancellationToken ct)
    {
        var appointmentId = command.AppointmentId;

        var appointment = await dbContext.Appointments
            .FirstOrDefaultAsync(a => a.Id == appointmentId, ct)
            ?? throw new NotFoundException("Không tìm thấy lịch hẹn.");

        appointment.SetFollowUpReminder(null, null);
        await dbContext.SaveChangesAsync(ct);

        return FollowUpReminderMapper.ToDto(appointmentId, null, null);
    }
}

/// <summary>
/// Danh sách bệnh nhân đang chờ tái khám: các buổi hẹn đã kết thúc điều trị mà BÁC SĨ
/// đã hẹn ngày tái khám (FollowUpDate) ở tab Tái khám. Bệnh nhân không cần đặt lịch lại —
/// staff check-in trực tiếp từ danh sách này.
/// Buổi gốc đã được check-in tái khám (có buổi con chưa hủy) sẽ được ẩn để tránh trùng.
/// </summary>
public class GetFollowUpDueHandler(AppDbContext dbContext) : IRequestHandler<GetFollowUpDueQuery, List<FollowUpDueDto>>
{
    public async Task<List<FollowUpDueDto>> Handle(GetFollowUpDueQuery request, CancellationToken ct)
    {
        // Buổi hẹn đã kết thúc điều trị và được bác sĩ hẹn tái khám.
        var scheduled = await dbContext.Appointments
            .AsNoTracking()
            .Include(a => a.Patient).ThenInclude(p => p.User)
            .Include(a => a.Dentist).ThenInclude(d => d.Employee).ThenInclude(e => e.User)
            .Include(a => a.Service)
            .Where(a => a.FollowUpDate != null &&
                        (a.Status == AppointmentStatus.Completed || a.Status == AppointmentStatus.PendingPayment))
            .ToListAsync(ct);

        if (scheduled.Count == 0) return new List<FollowUpDueDto>();

        var scheduledIds = scheduled.Select(a => a.Id).ToList();

        // Buổi gốc đã được check-in tái khám (buổi con chưa hủy) → ẩn.
        var checkedInSet = (await dbContext.Appointments
            .AsNoTracking()
            .Where(f => f.FollowUpFromAppointmentId != null &&
                        scheduledIds.Contains(f.FollowUpFromAppointmentId!.Value) &&
                        f.Status != AppointmentStatus.Cancelled)
            .Select(f => f.FollowUpFromAppointmentId!.Value)
            .ToListAsync(ct)).ToHashSet();

        var patientIds = scheduled.Select(a => a.PatientId).Distinct().ToList();

        // Liệu trình đang thực hiện (để hiển thị bối cảnh "đang điều trị", có thể rỗng).
        var activePlans = await dbContext.TreatmentPlans
            .AsNoTracking()
            .Include(tp => tp.Service)
            .Where(tp => tp.Status == TreatmentPlanStatus.InProgress && tp.AppointmentId != null && patientIds.Contains(tp.PatientId))
            .Select(tp => new { AppointmentId = tp.AppointmentId!.Value, ServiceName = tp.Service.Name })
            .ToListAsync(ct);

        // Bản đồ cha-con để gom liệu trình theo đúng chuỗi tái khám của mỗi buổi.
        var parentById = (await dbContext.Appointments
            .AsNoTracking()
            .Where(a => patientIds.Contains(a.PatientId))
            .Select(a => new { a.Id, a.FollowUpFromAppointmentId })
            .ToListAsync(ct))
            .ToDictionary(a => a.Id, a => a.FollowUpFromAppointmentId);

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
public class CheckInFollowUpHandler(AppDbContext dbContext) : IRequestHandler<CheckInFollowUpCommand, Guid>
{
    public async Task<Guid> Handle(CheckInFollowUpCommand command, CancellationToken ct)
    {
        var originalAppointmentId = command.OriginalAppointmentId;

        var original = await dbContext.Appointments
            .FirstOrDefaultAsync(a => a.Id == originalAppointmentId, ct)
            ?? throw new NotFoundException("Không tìm thấy buổi hẹn gốc.");

        // Chỉ check-in tái khám được khi bác sĩ đã hẹn ngày tái khám cho buổi này.
        if (original.FollowUpDate == null)
            throw new ValidationException("Buổi hẹn này chưa được bác sĩ hẹn tái khám.");

        // Chặn check-in tái khám lặp cho cùng một buổi gốc (buổi hủy không tính).
        // Các lịch hẹn/lượt khám khác của bệnh nhân là lần khám riêng — không ảnh hưởng.
        var alreadyCheckedIn = await dbContext.Appointments.AnyAsync(f =>
            f.FollowUpFromAppointmentId == originalAppointmentId && f.Status != AppointmentStatus.Cancelled, ct);
        if (alreadyCheckedIn)
            throw new ConflictException("Buổi hẹn này đã được check-in tái khám.");

        var followUpVisit = Appointment.CheckInFollowUp(
            original.Id,
            original.PatientId,
            original.DentistId,
            original.ServiceId,
            string.IsNullOrWhiteSpace(original.FollowUpNote) ? "Tái khám theo hẹn" : $"Tái khám: {original.FollowUpNote}");

        dbContext.Appointments.Add(followUpVisit);
        await dbContext.SaveChangesAsync(ct);

        return followUpVisit.Id;
    }
}
