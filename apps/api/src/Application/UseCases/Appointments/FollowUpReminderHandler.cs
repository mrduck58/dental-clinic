using DentalClinic.API.Domain.Entities;
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

/// <summary>Một bệnh nhân đang trong diện chờ tái khám (bác sĩ đã hẹn ngày tái khám sau khi kết thúc điều trị).</summary>
public class FollowUpDueDto
{
    public Guid OriginalAppointmentId { get; set; }
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string? PatientPhone { get; set; }
    public string? Gender { get; set; }
    public string DentistName { get; set; } = string.Empty;
    public string? ServiceName { get; set; }
    public DateTimeOffset OriginalAppointmentDate { get; set; }
    public DateOnly? FollowUpDate { get; set; }
    public string? FollowUpNote { get; set; }
    public List<string> ActivePlans { get; set; } = new(); // Các liệu trình đang thực hiện
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

        if (appointment.Status is not (AppointmentStatus.InProgress or AppointmentStatus.PendingPayment or AppointmentStatus.Completed))
            throw new ValidationException("Chỉ có thể hẹn tái khám khi buổi hẹn đang khám hoặc đã kết thúc điều trị.");

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

    /// <summary>
    /// Danh sách bệnh nhân đang chờ tái khám: các buổi hẹn đã kết thúc điều trị mà BÁC SĨ
    /// đã hẹn ngày tái khám (FollowUpDate) ở tab Tái khám. Bệnh nhân không cần đặt lịch lại —
    /// staff check-in trực tiếp từ danh sách này.
    /// Buổi gốc đã được check-in tái khám (có buổi con chưa hủy) sẽ được ẩn để tránh trùng.
    /// </summary>
    public async Task<List<FollowUpDueDto>> GetDueAsync(CancellationToken ct = default)
    {
        // Buổi hẹn đã kết thúc điều trị và được bác sĩ hẹn tái khám.
        var scheduled = await dbContext.Appointments
            .AsNoTracking()
            .Include(a => a.Patient).ThenInclude(p => p.User)
            .Include(a => a.Dentist).ThenInclude(d => d.User)
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

    /// <summary>
    /// Staff check-in bệnh nhân đến tái khám: tạo buổi hẹn mới đã check-in ngay,
    /// gắn về buổi gốc — bác sĩ sẽ thấy cờ tái khám và liệu trình cũ của bệnh nhân.
    /// </summary>
    public async Task<Guid> CheckInAsync(Guid originalAppointmentId, CancellationToken ct = default)
    {
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

    private static FollowUpReminderDto ToDto(Guid appointmentId, DateOnly? date, string? note) => new()
    {
        AppointmentId = appointmentId,
        FollowUpDate = date,
        FollowUpNote = note
    };
}
